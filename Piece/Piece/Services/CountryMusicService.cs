using Microsoft.EntityFrameworkCore;
using Piece.Data;
using Piece.Data.Models;

namespace Piece.Services
{
	public class CountryMusicService
	{
		private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

		public CountryMusicService(IDbContextFactory<ApplicationDbContext> dbFactory)
		{
			_dbFactory = dbFactory;
		}

		public async Task<CountryMusicData> GetCountryMusicData(string countryCode)
		{
			Console.WriteLine($"[CountryMusicService] Fetching data for: {countryCode}");

			using var context = await _dbFactory.CreateDbContextAsync();

			var country = await context.Countries
				.Include(c => c.Artists)
				.ThenInclude(a => a.ArtistTracks)
				.FirstOrDefaultAsync(c => c.CountryCode == countryCode);

			if (country == null)
			{
				Console.WriteLine($"[CountryMusicService] Country not found: {countryCode}");
				return new CountryMusicData { CountryName = countryCode };
			}

			Console.WriteLine($"[CountryMusicService] Found country: {country.Name} with {country.Artists.Count} artists");

			var artists = country.Artists.ToList();
			var allTracks = artists.SelectMany(a => a.ArtistTracks).ToList();

			Console.WriteLine($"[CountryMusicService] Total tracks: {allTracks.Count}");

			// Get top tracks this week (by popularity) - only if tracks exist
			var topTracks = allTracks
				.OrderByDescending(t => t.Popularity)
				.Take(10)
				.Select(t => new CountryTrackInfo
				{
					Id = t.Id,
					TrackName = t.TrackName,
					AlbumName = t.AlbumName,
					ArtistName = t.Artist?.Name ?? "Unknown",
					PreviewUrl = t.PreviewUrl,
					AlbumArtUrl = t.AlbumArtUrl,
					DurationSeconds = t.DurationSeconds,
					Popularity = t.Popularity
				})
				.ToList();

			// Get all artists ranked by popularity
			var featuredArtists = artists
				.Select(a => new CountryArtistInfo
				{
					Id = a.Id,
					Name = a.Name,
					ArtistName = a.Name,
					Bio = a.Bio,
					ImageUrl = a.ImageUrl,
					Genre = a.Genre,
					TrackCount = a.ArtistTracks.Count,
					AveragePopularity = a.Popularity > 0
						? a.Popularity
						: (a.ArtistTracks.Any() ? (int)a.ArtistTracks.Average(t => t.Popularity) : 0),
					TopTracks = a.ArtistTracks
						.OrderByDescending(t => t.Popularity)
						.Take(3)
						.Select(t => new CountryTrackInfo
						{
							Id = t.Id,
							TrackName = t.TrackName,
							AlbumName = t.AlbumName,
							ArtistName = a.Name,
							PreviewUrl = t.PreviewUrl,
							AlbumArtUrl = t.AlbumArtUrl,
							DurationSeconds = t.DurationSeconds,
							Popularity = t.Popularity
						})
						.ToList()
				})
				.OrderByDescending(a => a.AveragePopularity)
				.ThenByDescending(a => a.TrackCount)
				.ThenBy(a => a.Name)
				.Take(10)
				.ToList();

			Console.WriteLine($"[CountryMusicService] Featured artists: {featuredArtists.Count}");
			foreach (var artist in featuredArtists)
			{
				Console.WriteLine($"  - {artist.Name}: {artist.TopTracks.Count} tracks");
			}

			var result = new CountryMusicData
			{
				CountryName = country.Name,
				CountryCode = country.CountryCode,
				TotalArtists = artists.Count,
				TotalTracks = allTracks.Count,
				TopTracks = topTracks,
				FeaturedArtists = featuredArtists,
				Artists = featuredArtists, 
				TopGenres = artists
					.Where(a => !string.IsNullOrEmpty(a.Genre))
					.GroupBy(a => a.Genre)
					.OrderByDescending(g => g.Count())
					.Take(3)
					.Select(g => g.Key!)
					.ToList()
			};

			Console.WriteLine($"[CountryMusicService] Returning data with {result.Artists.Count} artists");

			return result;
		}
	}

	public class CountryMusicData
	{
		public string CountryName { get; set; } = string.Empty;
		public string CountryCode { get; set; } = string.Empty;
		public List<CountryArtistInfo> Artists { get; set; } = new(); 
		public int TotalArtists { get; set; }
		public int TotalTracks { get; set; }
		public List<CountryTrackInfo> TopTracks { get; set; } = new();
		public List<CountryArtistInfo> FeaturedArtists { get; set; } = new();
		public List<string> TopGenres { get; set; } = new();
	}

	public class CountryTrackInfo
	{
		public int Id { get; set; }
		public string TrackName { get; set; } = string.Empty;
		public string AlbumName { get; set; } = string.Empty;
		public string ArtistName { get; set; } = string.Empty;
		public string? PreviewUrl { get; set; }
		public string? AlbumArtUrl { get; set; }
		public int DurationSeconds { get; set; }
		public int Popularity { get; set; }
	}

	public class CountryArtistInfo
	{
		public int Id { get; set; }
		public string ArtistName { get; set; } = ""; // For Map.razor
		public string Name { get; set; } = string.Empty;
		public string? Bio { get; set; }
		public string? ImageUrl { get; set; }
		public string Genre { get; set; } = string.Empty;
		public int TrackCount { get; set; }
		public int AveragePopularity { get; set; }
		public List<CountryTrackInfo> TopTracks { get; set; } = new(); // Populated with artist's top tracks
	}
}