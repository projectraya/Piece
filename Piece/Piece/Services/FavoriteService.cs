using Microsoft.EntityFrameworkCore;
using Piece.Data;
using Piece.Data.Enums;
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
		Task<bool> ToggleExternalFavoriteAsync(string userId, TrackSource source, string externalId, string title, string artistName, string audioUrl, string? albumImage);
		Task<bool> IsExternalFavoriteAsync(string userId, TrackSource source, string externalId);
		Task<List<ExternalFavorite>> GetUserExternalFavoritesAsync(string userId, TrackSource? source = null);
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

		public async Task<bool> ToggleExternalFavoriteAsync(string userId, TrackSource source, string externalId, string title, string artistName, string audioUrl, string? albumImage)
		{
			var existing = await _dbContext.ExternalFavorites
				.FirstOrDefaultAsync(f => f.UserId == userId && f.Source == source && f.ExternalId == externalId);

			if (existing != null)
			{
				// Unlike - remove
				_dbContext.ExternalFavorites.Remove(existing);
				await _dbContext.SaveChangesAsync();
				return false;
			}
			else
			{
				// Like - add
				var favorite = new ExternalFavorite
				{
					UserId = userId,
					Source = source,
					ExternalId = externalId,
					Title = title,
					ArtistName = artistName,
					AlbumImage = albumImage,
					AudioUrl = audioUrl,
					LikedAt = DateTime.UtcNow
				};

				_dbContext.ExternalFavorites.Add(favorite);
				await _dbContext.SaveChangesAsync();
				return true;
			}
		}

		public async Task<bool> IsExternalFavoriteAsync(string userId, TrackSource source, string externalId)
		{
			return await _dbContext.ExternalFavorites
				.AnyAsync(f => f.UserId == userId && f.Source == source && f.ExternalId == externalId);
		}

		public async Task<List<ExternalFavorite>> GetUserExternalFavoritesAsync(string userId, TrackSource? source = null)
		{
			var query = _dbContext.ExternalFavorites.Where(f => f.UserId == userId);

			if (source.HasValue)
				query = query.Where(f => f.Source == source.Value);

			return await query
				.OrderByDescending(f => f.LikedAt)
				.ToListAsync();
		}
	}

}
