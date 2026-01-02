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
		private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

		public FavoriteService(IDbContextFactory<ApplicationDbContext> dbFactory)
		{
			_dbFactory = dbFactory;
		}
		public async Task<List<Track>> GetUserFavoritesAsync(string userId)
		{
			using var context = await _dbFactory.CreateDbContextAsync();
			return await context.UserTrackLikes
				.Where(x => x.UserId == userId)
				.Include(x => x.Track)
				.ThenInclude(t => t.Genre)
				.OrderByDescending(x => x.LikedAt)
				.Select(x => x.Track)
				.ToListAsync();
		}

		public async Task<List<int>> GetUserFavoriteTrackIdsAsync(string userId)
		{
			using var context = await _dbFactory.CreateDbContextAsync();
			return await context.UserTrackLikes
				.Where(x => x.UserId == userId)
				.Select(x => x.TrackId)
				.ToListAsync();
		}

		public async Task<bool> IsFavoriteAsync(string userId, int trackId)
		{
			using var context = await _dbFactory.CreateDbContextAsync();
			return await context.UserTrackLikes
				.AnyAsync(f => f.UserId == userId && f.TrackId == trackId);
		}

		public async Task<bool> ToggleFavoriteAsync(string userId, int trackId)
		{
			using var context = await _dbFactory.CreateDbContextAsync();
			var existing = await context.UserTrackLikes
				.FirstOrDefaultAsync(f => f.UserId == userId && f.TrackId == trackId);

			if(existing != null)
			{
				context.UserTrackLikes.Remove(existing);
				await context.SaveChangesAsync();
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

				context.UserTrackLikes.Add(favorite);
				await context.SaveChangesAsync();
				return true;
			}

		}

		public async Task<bool> ToggleExternalFavoriteAsync(string userId, TrackSource source, string externalId, string title, string artistName, string audioUrl, string? albumImage)
		{
			using var context = await _dbFactory.CreateDbContextAsync();
			var existing = await context.ExternalFavorites
				.FirstOrDefaultAsync(f => f.UserId == userId && f.Source == source && f.ExternalId == externalId);

			if (existing != null)
			{
				context.ExternalFavorites.Remove(existing);
				await context.SaveChangesAsync();
				return false;
			}
			else
			{
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

				context.ExternalFavorites.Add(favorite);
				await context.SaveChangesAsync();
				return true;
			}
		}

		public async Task<bool> IsExternalFavoriteAsync(string userId, TrackSource source, string externalId)
		{
			using var context = await _dbFactory.CreateDbContextAsync();
			return await context.ExternalFavorites
				.AnyAsync(f => f.UserId == userId && f.Source == source && f.ExternalId == externalId);
		}

		public async Task<List<ExternalFavorite>> GetUserExternalFavoritesAsync(string userId, TrackSource? source = null)
		{
			using var context = await _dbFactory.CreateDbContextAsync();
			var query = context.ExternalFavorites.Where(f => f.UserId == userId);

			if (source.HasValue)
				query = query.Where(f => f.Source == source.Value);

			return await query
				.OrderByDescending(f => f.LikedAt)
				.ToListAsync();
		}
	}

}
