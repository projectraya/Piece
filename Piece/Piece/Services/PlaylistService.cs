using Piece.Data.Models;
using Piece.Data;
using Microsoft.EntityFrameworkCore;

namespace Piece.Services
{
	public interface IPlaylistService
	{
		Task<List<Playlist>> GetUserPlaylistsAsync(string userId);
		Task<Playlist?> GetPlaylistByIdAsync(int id, string userId);
		Task<Playlist> CreatePlaylistAsync(string userId, string name, string? description = null, bool isPublic = true);
		Task<bool> UpdatePlaylistAsync(int id, string userId, string name, string? description = null, bool? isPublic = null);
		Task<bool> DeletePlaylistAsync(int id, string userId);
		Task<bool> AddTrackToPlaylistAsync(int playlistId, int trackId, string userId);
		Task<bool> RemoveTrackFromPlaylistAsync(int playlistId, int trackId, string userId);
		Task<bool> ReorderTracksAsync(int playlistId, List<int> trackIds, string userId);
		Task<List<Track>> GetPlaylistTracksAsync(int playlistId, string userId);
	}

	public class PlaylistService : IPlaylistService
	{
		private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

		public PlaylistService(IDbContextFactory<ApplicationDbContext> dbFactory)
		{
			_dbFactory = dbFactory;
		}

		public async Task<List<Playlist>> GetUserPlaylistsAsync(string userId)
		{
			using var context = await _dbFactory.CreateDbContextAsync();
			return await context.Playlists
				.Where(p => p.UserId == userId)
				.Include(p => p.PlaylistTracks)
				.OrderByDescending(p => p.UpdatedAt)
				.ToListAsync();
		}

		public async Task<Playlist?> GetPlaylistByIdAsync(int id, string userId)
		{
			using var context = await _dbFactory.CreateDbContextAsync();
			return await context.Playlists
				.Include(p => p.PlaylistTracks)
					.ThenInclude(pt => pt.Track)
						.ThenInclude(t => t.Genre)
				.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
		}

		public async Task<Playlist> CreatePlaylistAsync(string userId, string name, string? description = null, bool isPublic = true)
		{
			using var context = await _dbFactory.CreateDbContextAsync();
			var playlist = new Playlist
			{
				UserId = userId,
				Name = name,
				Description = description,
				IsPublic = isPublic,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};

			context.Playlists.Add(playlist);
			await context.SaveChangesAsync();

			return playlist;
		}

		public async Task<bool> UpdatePlaylistAsync(int id, string userId, string name, string? description = null, bool? isPublic = null)
		{
			using var context = await _dbFactory.CreateDbContextAsync();
			var playlist = await context.Playlists.FindAsync(id);
			if (playlist == null || playlist.UserId != userId)
				return false;

			playlist.Name = name;
			playlist.Description = description;
			if (isPublic.HasValue)
				playlist.IsPublic = isPublic.Value;
			playlist.UpdatedAt = DateTime.UtcNow;

			await context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> DeletePlaylistAsync(int id, string userId)
		{
			using var context = await _dbFactory.CreateDbContextAsync();
			var playlist = await context.Playlists.FindAsync(id);
			if (playlist == null || playlist.UserId != userId)
				return false;

			context.Playlists.Remove(playlist);
			await context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> AddTrackToPlaylistAsync(int playlistId, int trackId, string userId)
		{
			using var context = await _dbFactory.CreateDbContextAsync();
			var playlist = await context.Playlists
				.Include(p => p.PlaylistTracks)
				.FirstOrDefaultAsync(p => p.Id == playlistId && p.UserId == userId);

			if (playlist == null)
				return false;

			var track = await context.Tracks.FindAsync(trackId);
			if (track == null || !track.IsActive)
				return false;

			if (playlist.PlaylistTracks.Any(pt => pt.TrackId == trackId))
				return false;

			var maxPosition = playlist.PlaylistTracks.Any()
				? playlist.PlaylistTracks.Max(pt => pt.Position)
				: 0;

			var playlistTrack = new PlaylistTrack
			{
				PlaylistId = playlistId,
				TrackId = trackId,
				Position = maxPosition + 1,
				AddedAt = DateTime.UtcNow
			};

			context.PlaylistTracks.Add(playlistTrack);
			playlist.UpdatedAt = DateTime.UtcNow;

			await context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> RemoveTrackFromPlaylistAsync(int playlistId, int trackId, string userId)
		{
			using var context = await _dbFactory.CreateDbContextAsync();
			var playlist = await context.Playlists.FindAsync(playlistId);
			if (playlist == null || playlist.UserId != userId)
				return false;

			var playlistTrack = await context.PlaylistTracks
				.FirstOrDefaultAsync(pt => pt.PlaylistId == playlistId && pt.TrackId == trackId);

			if (playlistTrack == null)
				return false;

			context.PlaylistTracks.Remove(playlistTrack);
			playlist.UpdatedAt = DateTime.UtcNow;

			await context.SaveChangesAsync();

			await ReorderTracksAfterRemoval(playlistId, playlistTrack.Position);

			return true;
		}

		public async Task<bool> ReorderTracksAsync(int playlistId, List<int> trackIds, string userId)
		{
			using var context = await _dbFactory.CreateDbContextAsync();
			var playlist = await context.Playlists
				.Include(p => p.PlaylistTracks)
				.FirstOrDefaultAsync(p => p.Id == playlistId && p.UserId == userId);

			if (playlist == null)
				return false;

			for (int i = 0; i < trackIds.Count; i++)
			{
				var playlistTrack = playlist.PlaylistTracks
					.FirstOrDefault(pt => pt.TrackId == trackIds[i]);

				if (playlistTrack != null)
				{
					playlistTrack.Position = i + 1;
				}
			}

			playlist.UpdatedAt = DateTime.UtcNow;
			await context.SaveChangesAsync();

			return true;
		}

		public async Task<List<Track>> GetPlaylistTracksAsync(int playlistId, string userId)
		{
			using var context = await _dbFactory.CreateDbContextAsync();
			var playlist = await context.Playlists.FindAsync(playlistId);
			if (playlist == null || playlist.UserId != userId)
				return new List<Track>();

			var playlistTracks = await context.PlaylistTracks
				.Include(pt => pt.Track)
					.ThenInclude(t => t.Genre)
				.Where(pt => pt.PlaylistId == playlistId)
				.OrderBy(pt => pt.Position)
				.Select(pt => pt.Track)
				.ToListAsync();

			return playlistTracks;
		}

		private async Task ReorderTracksAfterRemoval(int playlistId, int removedPosition)
		{
			using var context = await _dbFactory.CreateDbContextAsync();
			var tracksToReorder = await context.PlaylistTracks
				.Where(pt => pt.PlaylistId == playlistId && pt.Position > removedPosition)
				.ToListAsync();

			foreach (var track in tracksToReorder)
			{
				track.Position--;
			}

			await context.SaveChangesAsync();
		}
	}
}
