using Microsoft.EntityFrameworkCore;
using Piece.Data;
using Piece.Data.Models;
using System.Linq.Expressions;

namespace Piece.Services
{
	public interface IFavoriteService
	{
		Task<bool> ToggleFavoriteAsync(string userId, int trackId);
		Task<bool> IsFavoriteAsync(string userId, int trackId);
		Task<List<Track>> GetUserFavoritesAsync(string userId);
		Task<List<int>> GetUserFavoriteTrackIdsAsync(string userId);
	}
	public class FavoriteService : IFavoriteService
	{
		private readonly ApplicationDbContext _dbContext;

		public FavoriteService(ApplicationDbContext dbContext)
		{
			_dbContext = dbContext;
		}
		public async Task<List<Track>> GetUserFavoritesAsync(string userId)
		{
			return await _dbContext.UserTrackLikes
				.Where(x => x.UserId == userId)
				.Include(x => x.Track)
				.ThenInclude(t => t.Genre)
				.OrderByDescending(x => x.LikedAt)
				.Select(x => x.Track)
				.ToListAsync();
		}

		public async Task<List<int>> GetUserFavoriteTrackIdsAsync(string userId)
		{
			return await _dbContext.UserTrackLikes
				.Where(x => x.UserId == userId)
				.Select(x => x.TrackId)
				.ToListAsync();
		}

		public async Task<bool> IsFavoriteAsync(string userId, int trackId)
		{
			return await _dbContext.UserTrackLikes
				.AnyAsync(f => f.UserId == userId && f.TrackId == trackId);
		}

		public async Task<bool> ToggleFavoriteAsync(string userId, int trackId)
		{
			var existing = await _dbContext.UserTrackLikes
				.FirstOrDefaultAsync(f => f.UserId == userId && f.TrackId == trackId);

			if(existing != null)
			{
				_dbContext.UserTrackLikes.Remove(existing);
				await _dbContext.SaveChangesAsync();
				return false;
			}
			else
			{
				var favorite = new UserFavorites
				{
					UserId = userId,
					TrackId = trackId,
					LikedAt = DateTime.UtcNow

				};

				_dbContext.UserTrackLikes.Add(favorite);
				await _dbContext.SaveChangesAsync();
				return true;
			}

		}
	}

}
