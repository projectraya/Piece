using Microsoft.EntityFrameworkCore;
using Piece.Data;
using Piece.Data.Models;

namespace Piece.Services
{
	public interface IProfileService
	{
		Task<ApplicationUser?> GetUserProfileAsync(string userId);
		Task<ApplicationUser?> GetUserProfileByUsernameAsync(string username);
		Task<ApplicationUser?> GetUserProfileByEmailAsync(string email);
		Task<bool> UpdateProfileAsync(string userId, string? displayName, string? bio, bool isPublic, bool showHistory, bool showPlaylists);
		Task<List<ApplicationUser>> SearchUsersAsync(string searchQuery, int limit = 20);
		Task<List<Playlist>> GetUserPublicPlaylistsAsync(string userId);
		Task UpdateLastActiveAsync(string userId);
	}

	public class ProfileService : IProfileService
	{
		private readonly ApplicationDbContext _context;

		public ProfileService(ApplicationDbContext context)
		{
			_context = context;
		}

		public async Task<ApplicationUser?> GetUserProfileAsync(string userId)
		{
			return await _context.Users
				.Include(u => u.Playlists.Where(p => p.IsPublic))
					.ThenInclude(p => p.PlaylistTracks)
				.FirstOrDefaultAsync(u => u.Id == userId);
		}

		public async Task<ApplicationUser?> GetUserProfileByUsernameAsync(string username)
		{
			return await _context.Users
				.Include(u => u.Playlists.Where(p => p.IsPublic))
					.ThenInclude(p => p.PlaylistTracks)
				.FirstOrDefaultAsync(u => u.UserName == username);
		}

		public async Task<ApplicationUser?> GetUserProfileByEmailAsync(string email)
		{
			return await _context.Users
				.Include(u => u.Playlists.Where(p => p.IsPublic))
					.ThenInclude(p => p.PlaylistTracks)
				.FirstOrDefaultAsync(u => u.Email == email);
		}

		public async Task<bool> UpdateProfileAsync(string userId, string? displayName, string? bio, bool isPublic, bool showHistory, bool showPlaylists)
		{
			var user = await _context.Users.FindAsync(userId);
			if (user == null)
				return false;

			user.DisplayName = displayName?.Trim();
			user.Bio = bio?.Trim();
			user.IsProfilePublic = isPublic;
			user.ShowListeningHistory = showHistory;
			user.ShowPlaylists = showPlaylists;

			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<List<ApplicationUser>> SearchUsersAsync(string searchQuery, int limit = 20)
		{
			if (string.IsNullOrWhiteSpace(searchQuery))
				return new List<ApplicationUser>();

			var query = searchQuery.ToLower().Trim();

			return await _context.Users
				.Where(u => u.IsProfilePublic &&
						   (u.DisplayName != null && u.DisplayName.ToLower().Contains(query) ||
							u.Email != null && u.Email.ToLower().Contains(query)))
				.Take(limit)
				.ToListAsync();
		}

		public async Task<List<Playlist>> GetUserPublicPlaylistsAsync(string userId)
		{
			var user = await _context.Users
				.Include(u => u.Playlists.Where(p => p.IsPublic))
					.ThenInclude(p => p.PlaylistTracks)
						.ThenInclude(pt => pt.Track)
				.FirstOrDefaultAsync(u => u.Id == userId);

			if (user == null || !user.ShowPlaylists)
				return new List<Playlist>();

			return user.Playlists.ToList();
		}

		public async Task UpdateLastActiveAsync(string userId)
		{
			var user = await _context.Users.FindAsync(userId);
			if (user != null)
			{
				user.LastActiveAt = DateTime.UtcNow;
				await _context.SaveChangesAsync();
			}
		}
	}
}