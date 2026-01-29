using Microsoft.EntityFrameworkCore;
using Piece.Data;
using Piece.Data.Models;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Piece.Services
{
	public class CountryMusicService
	{
		private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
		private readonly HttpClient _httpClient;
		private readonly DeezerService _deezerService;

		public CountryMusicService(
			IDbContextFactory<ApplicationDbContext> dbFactory,
			IHttpClientFactory httpClientFactory,
			DeezerService deezerService) 
		{
			_dbFactory = dbFactory;
			_httpClient = httpClientFactory.CreateClient();
			_httpClient.DefaultRequestHeaders.Add("User-Agent", "Piece/1.0 (music-discovery-app)");
			_deezerService = deezerService; 
		}

		public async Task<CountryMusicData> GetCountryMusicData(string countryCode)
		{
			Console.WriteLine($"[CountryMusicService] Fetching data for: {countryCode}");

			using var context = await _dbFactory.CreateDbContextAsync();

			var country = await context.Countries
				.FirstOrDefaultAsync(c => c.CountryCode == countryCode);

			if (country == null)
			{
				Console.WriteLine($"[CountryMusicService] Country not found: {countryCode}");
				return new CountryMusicData { CountryName = countryCode };
			}

			Console.WriteLine($"[CountryMusicService] Found country: {country.Name}");

			var artistsFromCountry = await GetArtistsFromCountryViaMusicBrainz(country.Name);

			if (artistsFromCountry.Count == 0)
			{
				Console.WriteLine($"[CountryMusicService] No MusicBrainz artists found, falling back to local database");
				
				return await GetCountryMusicDataFromDatabase(context, country);
			}

			Console.WriteLine($"[CountryMusicService] Returning {artistsFromCountry.Count} artists from MusicBrainz");

			return new CountryMusicData
			{
				CountryName = country.Name,
				CountryCode = country.CountryCode,
				TotalArtists = artistsFromCountry.Count,
				TotalTracks = 0,
				TopTracks = new List<CountryTrackInfo>(),
				FeaturedArtists = artistsFromCountry,
				Artists = artistsFromCountry,
				TopGenres = artistsFromCountry
					.Where(a => !string.IsNullOrEmpty(a.Genre))
					.GroupBy(a => a.Genre)
					.OrderByDescending(g => g.Count())
					.Take(3)
					.Select(g => g.Key!)
					.ToList()
			};
		}

		private async Task<List<CountryArtistInfo>> GetArtistsFromCountryViaMusicBrainz(string countryName)
		{
			try
			{
				var encodedCountry = Uri.EscapeDataString(countryName);
				var url = $"https://musicbrainz.org/ws/2/artist?query=area:\"{encodedCountry}\" AND (type:person OR type:group) AND NOT type:character&limit=100&fmt=json";

				Console.WriteLine($"[CountryMusicService] MusicBrainz query: {url}");

				await Task.Delay(1100);

				var response = await _httpClient.GetFromJsonAsync<MusicBrainzResponse>(url);

				if (response?.Artists == null || !response.Artists.Any())
				{
					Console.WriteLine($"[CountryMusicService] No artists found for: {countryName}");
					return new List<CountryArtistInfo>();
				}

				var filteredArtists = response.Artists
					.Where(a => !string.IsNullOrEmpty(a.Name) && IsLikelyMusician(a))
					.Take(20)
					.ToList();

				Console.WriteLine($"[CountryMusicService] Filtered to {filteredArtists.Count} musicians from MusicBrainz");

				var artistsList = new List<CountryArtistInfo>();
				int index = 0;

				foreach (var mbArtist in filteredArtists)
				{
					Console.WriteLine($"[CountryMusicService] Fetching Deezer tracks for: {mbArtist.Name}");

					var deezerTracks = await _deezerService.GetArtistTopTracksAsync(mbArtist.Name!, 3);

					if (!deezerTracks.Any())
					{
						Console.WriteLine($"[CountryMusicService] No Deezer tracks for {mbArtist.Name}, skipping");
						continue;
					}

					var artist = new CountryArtistInfo
					{
						Id = index,
						Name = mbArtist.Name ?? "Unknown Artist",
						ArtistName = mbArtist.Name ?? "Unknown Artist",
						Bio = mbArtist.Disambiguation,
						ImageUrl = deezerTracks.FirstOrDefault()?.Album?.CoverBig, 
						Genre = GetGenreFromType(mbArtist.Type) ?? GetGenreFromTags(mbArtist.Tags),
						TrackCount = deezerTracks.Count,
						AveragePopularity = 100 - (index * 2),
						TopTracks = deezerTracks.Select(dt => new CountryTrackInfo
						{
							Id = 0,
							TrackName = dt.Title,
							AlbumName = dt.Album?.Title ?? "",
							ArtistName = mbArtist.Name ?? "Unknown Artist",
							PreviewUrl = dt.Preview, 
							AlbumArtUrl = dt.Album?.CoverMedium,
							DurationSeconds = dt.Duration,
							Popularity = 0
						}).ToList()
					};

					artistsList.Add(artist);
					index++;

					Console.WriteLine($"[CountryMusicService] ✓ Added {mbArtist.Name} with {deezerTracks.Count} preview tracks");

					await Task.Delay(200);
				}

				Console.WriteLine($"[CountryMusicService] Final result: {artistsList.Count} artists with Deezer previews");
				return artistsList;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[CountryMusicService] Error fetching from MusicBrainz: {ex.Message}");
				return new List<CountryArtistInfo>();
			}
		}

		private bool IsLikelyMusician(MusicBrainzArtist artist)
		{
			// Filter out non-musicians based on tags and disambiguation
			var disambiguation = artist.Disambiguation?.ToLower() ?? "";

			// Exclude writers, authors, producers (unless music producer)
			var excludeKeywords = new[] { "writer", "author", "novelist", "poet", "journalist", "actor", "director" };
			if (excludeKeywords.Any(keyword => disambiguation.Contains(keyword)))
			{
				return false;
			}

			// If they have music-related tags, they're likely a musician
			if (artist.Tags != null && artist.Tags.Any())
			{
				var musicTags = new[] { "rock", "pop", "jazz", "classical", "hip hop", "electronic", "metal", "folk", "country", "blues", "soul", "r&b", "rap", "punk", "indie" };
				if (artist.Tags.Any(tag => musicTags.Any(mt => tag.Name?.ToLower().Contains(mt) == true)))
				{
					return true;
				}
			}

			return artist.Type == "Group" || artist.Type == "Person";
		}

		private async Task<CountryMusicData> GetCountryMusicDataFromDatabase(ApplicationDbContext context, Country country)
		{
			var artists = await context.Artists
				.Where(a => a.CountryId == country.Id)
				.Include(a => a.ArtistTracks)
				.ToListAsync();

			var allTracks = artists.SelectMany(a => a.ArtistTracks).ToList();

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
				.Take(10)
				.ToList();

			return new CountryMusicData
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
		}

		private string? GetGenreFromType(string? type)
		{
			return type switch
			{
				"Person" => "Solo Artist",
				"Group" => "Band",
				"Orchestra" => "Classical",
				"Choir" => "Choral",
				_ => null
			};
		}

		private string GetGenreFromTags(List<MusicBrainzTag>? tags)
		{
			if (tags == null || !tags.Any())
				return "Various";

			var genreTags = tags
				.Where(t => !string.IsNullOrEmpty(t.Name))
				.OrderByDescending(t => t.Count)
				.FirstOrDefault();

			return genreTags?.Name?.Trim() ?? "Various";
		}

		private class MusicBrainzResponse
		{
			[JsonPropertyName("artists")]
			public List<MusicBrainzArtist>? Artists { get; set; }
		}

		private class MusicBrainzArtist
		{
			[JsonPropertyName("id")]
			public string? Id { get; set; }

			[JsonPropertyName("name")]
			public string? Name { get; set; }

			[JsonPropertyName("type")]
			public string? Type { get; set; }

			[JsonPropertyName("country")]
			public string? Country { get; set; }

			[JsonPropertyName("disambiguation")]
			public string? Disambiguation { get; set; }

			[JsonPropertyName("score")]
			public int? Score { get; set; }

			[JsonPropertyName("tags")]
			public List<MusicBrainzTag>? Tags { get; set; }
		}

		private class MusicBrainzTag
		{
			[JsonPropertyName("name")]
			public string? Name { get; set; }

			[JsonPropertyName("count")]
			public int Count { get; set; }
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
		public string ArtistName { get; set; } = "";
		public string Name { get; set; } = string.Empty;
		public string? Bio { get; set; }
		public string? ImageUrl { get; set; }
		public string Genre { get; set; } = string.Empty;
		public int TrackCount { get; set; }
		public int AveragePopularity { get; set; }
		public List<CountryTrackInfo> TopTracks { get; set; } = new();
	}
}