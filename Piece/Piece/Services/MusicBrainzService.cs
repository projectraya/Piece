using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Piece.Data;
using Piece.Data.Models;
using Piece.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace Piece.Services
{
	public class MusicBrainzService
	{
		private readonly HttpClient _httpClient;
		private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
		private const string BaseUrl = "https://musicbrainz.org/ws/2";

		public MusicBrainzService(HttpClient httpClient, IDbContextFactory<ApplicationDbContext> dbFactory)
		{
			_httpClient = httpClient;
			_dbFactory = dbFactory;
			_httpClient.DefaultRequestHeaders.Add("User-Agent", "Piece/1.0 (rayapetkova@gmail.com)");
		}

		public async Task<List<ArtistInfo>> SearchArtistsByCountry(string countryName, int limit = 100)
		{
			try
			{
				// MusicBrainz uses country NAME in area search, not code
				// Also need to escape special characters and use proper query syntax
				var encodedCountry = Uri.EscapeDataString(countryName);
				var url = $"{BaseUrl}/artist?query=area:\"{encodedCountry}\"&limit={limit}&fmt=json";

				Console.WriteLine($"   📡 MusicBrainz query: {url}");

				await Task.Delay(1100); // MusicBrainz rate limit: 1 request per second (add buffer)

				var response = await _httpClient.GetFromJsonAsync<MusicBrainzSearchResponse>(url);

				if (response?.Artists == null || response.Artists.Count == 0)
				{
					Console.WriteLine($"   ⚠️ No artists found for area: {countryName}");
					return new List<ArtistInfo>();
				}

				Console.WriteLine($"   ✓ Got {response.Artists.Count} artists from {countryName}");

				return response.Artists.Select(a => new ArtistInfo
				{
					Name = a.Name ?? "Unknown Artist",
					MusicBrainzId = a.Id,
					Country = a.Country ?? a.Area?.Name ?? a.BeginArea?.Name,
					Genre = a.Type ?? "Unknown",
					Bio = a.Disambiguation
				}).ToList();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"   ❌ Error searching MusicBrainz: {ex.Message}");
				return new List<ArtistInfo>();
			}
		}

		public async Task SeedArtistsForCountry(int countryId, string countryName, int limit = 30)
		{
			try
			{
				Console.WriteLine($"Calling MusicBrainz API for {countryName}...");
				var artists = await SearchArtistsByCountry(countryName, limit);
				Console.WriteLine($"MusicBrainz returned {artists.Count} artists");

				using var context = await _dbFactory.CreateDbContextAsync();
				foreach (var artistInfo in artists)
				{
					// Check if artist already exists
					var existingArtist = await context.Artists
						.FirstOrDefaultAsync(a => a.MusicBrainzId == artistInfo.MusicBrainzId);

					if (existingArtist == null)
					{
						var newArtist = new Artist
						{
							Name = artistInfo.Name,
							Bio = artistInfo.Bio,
							Genre = artistInfo.Genre,
							CountryId = countryId,
							DataSource = ArtistDataSource.MusicBrainz,
							MusicBrainzId = artistInfo.MusicBrainzId,
							CreatedAt = DateTime.UtcNow
						};
						context.Artists.Add(newArtist);
					}
				}
				await context.SaveChangesAsync();
				Console.WriteLine($"Seeded {artists.Count} artists for {countryName}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error seeding artists: {ex.Message}");
			}
		}

		private class MusicBrainzSearchResponse
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

			[JsonPropertyName("country")]
			public string? Country { get; set; }

			[JsonPropertyName("type")]
			public string? Type { get; set; }

			[JsonPropertyName("disambiguation")]
			public string? Disambiguation { get; set; }

			[JsonPropertyName("area")]
			public MusicBrainzArea? Area { get; set; }

			[JsonPropertyName("begin-area")]
			public MusicBrainzArea? BeginArea { get; set; }
		}

		private class MusicBrainzArea
		{
			[JsonPropertyName("name")]
			public string? Name { get; set; }
		}
	}

	public class ArtistInfo
	{
		public string Name { get; set; } = string.Empty;
		public string? MusicBrainzId { get; set; }
		public string? Country { get; set; }
		public string Genre { get; set; } = string.Empty;
		public string? Bio { get; set; }
	}
}