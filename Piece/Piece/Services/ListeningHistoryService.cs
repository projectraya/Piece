using Microsoft.EntityFrameworkCore;
using Piece.Data;
using Piece.Data.Models;

namespace Piece.Services
{
	public interface IListeningHistoryService
	{
		Task RecordListeningAsync(string userId, int trackId, int durationSeconds);
		Task<Dictionary<DateTime, List<(string Color, int Count)>>> GetGenreHistoryAsync(string userId, DateTime startDate, DateTime endDate);
		Task<Dictionary<string, (int Count, string Color)>> GetTopGenresAsync(string userId, int days = 30);
	}

	public class ListeningHistoryService : IListeningHistoryService
	{
		private readonly ApplicationDbContext _context;

		public ListeningHistoryService(ApplicationDbContext context)
		{
			_context = context;
		}

		public async Task RecordListeningAsync(string userId, int trackId, int durationSeconds)
		{
			var history = new ListeningHistory
			{
				UserId = userId,
				TrackId = trackId,
				PlayedAt = DateTime.UtcNow,
				DurationListened = durationSeconds
			};

			_context.ListeningHistory.Add(history);
			await _context.SaveChangesAsync();

			Console.WriteLine($"[ListeningHistoryService] Recorded {durationSeconds}s for track {trackId}");
		}

		public async Task<Dictionary<DateTime, List<(string Color, int Count)>>> GetGenreHistoryAsync(string userId, DateTime startDate, DateTime endDate)
		{
			Console.WriteLine($"[ListeningHistoryService] Query range: {startDate:yyyy-MM-dd HH:mm:ss} to {endDate:yyyy-MM-dd HH:mm:ss}");

			var endDateTime = endDate.AddDays(1).AddSeconds(-1);

			var history = await _context.ListeningHistory
				.Where(h => h.UserId == userId && h.PlayedAt >= startDate && h.PlayedAt <= endDateTime)
				.Include(h => h.Track)
					.ThenInclude(t => t.Genre)
				.ToListAsync();

			Console.WriteLine($"[ListeningHistoryService] Found {history.Count} listening records");

			// Group by date AND genre to get counts per genre per day
			var dailyGenres = history
				.Where(h => h.Track.Genre != null)
				.GroupBy(h => h.PlayedAt.Date)
				.ToDictionary(
					g => g.Key,
					g => {
						// Count plays per genre for this day
						var genreCounts = g
							.GroupBy(h => h.Track.Genre!)
							.Select(genreGroup => (
								Color: genreGroup.Key.Color ?? "#667eea",
								Count: genreGroup.Count()
							))
							.ToList();

						Console.WriteLine($"[ListeningHistoryService] Day {g.Key:yyyy-MM-dd}: {string.Join(", ", genreCounts.Select(x => $"{x.Color}({x.Count})"))}");
						return genreCounts;
					}
				);

			Console.WriteLine($"[ListeningHistoryService] Returning {dailyGenres.Count} days with data");
			return dailyGenres;
		}

		public async Task<Dictionary<string, (int Count, string Color)>> GetTopGenresAsync(string userId, int days = 30)
		{
			var startDate = DateTime.UtcNow.AddDays(-days);

			var topGenres = await _context.ListeningHistory
				.Where(h => h.UserId == userId && h.PlayedAt >= startDate)
				.Include(h => h.Track)
					.ThenInclude(t => t.Genre)
				.Where(h => h.Track.Genre != null)
				.GroupBy(h => new { h.Track.Genre!.Name, h.Track.Genre.Color })
				.Select(g => new
				{
					Genre = g.Key.Name,
					Color = g.Key.Color ?? "#667eea",
					Count = g.Count()
				})
				.OrderByDescending(x => x.Count)
				.ToDictionaryAsync(x => x.Genre, x => (x.Count, x.Color));

			Console.WriteLine($"[ListeningHistoryService] Top genres: {string.Join(", ", topGenres.Keys)}");

			return topGenres;
		}
	}
}