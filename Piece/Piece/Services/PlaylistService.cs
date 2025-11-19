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
		private readonly ApplicationDbContext _context;

		public PlaylistService(ApplicationDbContext context)
		{
			_context = context;
		}

		public async Task<List<Playlist>> GetUserPlaylistsAsync(string userId)
		{
			return await _context.Playlists
				.Where(p => p.UserId == userId)
				.Include(p => p.PlaylistTracks)
				.OrderByDescending(p => p.UpdatedAt)
				.ToListAsync();
		}

		public async Task<Playlist?> GetPlaylistByIdAsync(int id, string userId)
		{
			return await _context.Playlists
				.Include(p => p.PlaylistTracks)
					.ThenInclude(pt => pt.Track)
						.ThenInclude(t => t.Genre)
				.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
		}

		public async Task<Playlist> CreatePlaylistAsync(string userId, string name, string? description = null, bool isPublic = true)
		{
			var playlist = new Playlist
			{
				UserId = userId,
				Name = name,
				Description = description,
				IsPublic = isPublic,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};

			_context.Playlists.Add(playlist);
			await _context.SaveChangesAsync();

			return playlist;
		}

		public async Task<bool> UpdatePlaylistAsync(int id, string userId, string name, string? description = null, bool? isPublic = null)
		{
			var playlist = await _context.Playlists.FindAsync(id);
			if (playlist == null || playlist.UserId != userId)
				return false;

			playlist.Name = name;
			playlist.Description = description;
			if (isPublic.HasValue)
				playlist.IsPublic = isPublic.Value;
			playlist.UpdatedAt = DateTime.UtcNow;

			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> DeletePlaylistAsync(int id, string userId)
		{
			var playlist = await _context.Playlists.FindAsync(id);
			if (playlist == null || playlist.UserId != userId)
				return false;

			// Hard delete
			_context.Playlists.Remove(playlist);
			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> AddTrackToPlaylistAsync(int playlistId, int trackId, string userId)
		{
			var playlist = await _context.Playlists
				.Include(p => p.PlaylistTracks)
				.FirstOrDefaultAsync(p => p.Id == playlistId && p.UserId == userId);

			if (playlist == null)
				return false;

			var track = await _context.Tracks.FindAsync(trackId);
			if (track == null || !track.IsActive)
				return false;

			// Check if track already exists in playlist
			if (playlist.PlaylistTracks.Any(pt => pt.TrackId == trackId))
				return false;

			// Get the next position
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

			_context.PlaylistTracks.Add(playlistTrack);
			playlist.UpdatedAt = DateTime.UtcNow;

			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> RemoveTrackFromPlaylistAsync(int playlistId, int trackId, string userId)
		{
			var playlist = await _context.Playlists.FindAsync(playlistId);
			if (playlist == null || playlist.UserId != userId)
				return false;

			var playlistTrack = await _context.PlaylistTracks
				.FirstOrDefaultAsync(pt => pt.PlaylistId == playlistId && pt.TrackId == trackId);

			if (playlistTrack == null)
				return false;

			_context.PlaylistTracks.Remove(playlistTrack);
			playlist.UpdatedAt = DateTime.UtcNow;

			await _context.SaveChangesAsync();

			// Reorder remaining tracks
			await ReorderTracksAfterRemoval(playlistId, playlistTrack.Position);

			return true;
		}

		public async Task<bool> ReorderTracksAsync(int playlistId, List<int> trackIds, string userId)
		{
			var playlist = await _context.Playlists
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
			await _context.SaveChangesAsync();

			return true;
		}

		public async Task<List<Track>> GetPlaylistTracksAsync(int playlistId, string userId)
		{
			var playlist = await _context.Playlists.FindAsync(playlistId);
			if (playlist == null || playlist.UserId != userId)
				return new List<Track>();

			var playlistTracks = await _context.PlaylistTracks
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
			var tracksToReorder = await _context.PlaylistTracks
				.Where(pt => pt.PlaylistId == playlistId && pt.Position > removedPosition)
				.ToListAsync();

			foreach (var track in tracksToReorder)
			{
				track.Position--;
			}

			await _context.SaveChangesAsync();
		}
	}
}
