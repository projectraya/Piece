using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Piece.Services
{
	public class DeezerService
	{
		private readonly HttpClient _httpClient;

		public DeezerService(HttpClient httpClient)
		{
			_httpClient = httpClient;
			_httpClient.BaseAddress = new Uri("https://api.deezer.com/");
		}

		public async Task<List<DeezerTrack>> GetArtistTopTracksAsync(string artistName, int limit = 3)
		{
			try
			{
				// Step 1: Search for the artist
				var searchUrl = $"search/artist?q={Uri.EscapeDataString(artistName)}&limit=1";
				var searchResponse = await _httpClient.GetFromJsonAsync<DeezerSearchResponse>(searchUrl);

				if (searchResponse?.Data == null || !searchResponse.Data.Any())
				{
					Console.WriteLine($"[Deezer] No artist found for: {artistName}");
					return new List<DeezerTrack>();
				}

				var artistId = searchResponse.Data[0].Id;

				// Step 2: Get artist's top tracks
				var tracksUrl = $"artist/{artistId}/top?limit={limit}";
				var tracksResponse = await _httpClient.GetFromJsonAsync<DeezerTracksResponse>(tracksUrl);

				if (tracksResponse?.Data == null)
				{
					Console.WriteLine($"[Deezer] No tracks found for: {artistName}");
					return new List<DeezerTrack>();
				}

				var tracksWithPreviews = tracksResponse.Data
					.Where(t => !string.IsNullOrEmpty(t.Preview))
					.ToList();

				Console.WriteLine($"[Deezer] Found {tracksWithPreviews.Count} tracks with previews for: {artistName}");
				return tracksWithPreviews;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[Deezer] Error getting tracks for {artistName}: {ex.Message}");
				return new List<DeezerTrack>();
			}
		}

		// Response models
		private class DeezerSearchResponse
		{
			[JsonPropertyName("data")]
			public List<DeezerArtist>? Data { get; set; }
		}

		private class DeezerArtist
		{
			[JsonPropertyName("id")]
			public long Id { get; set; }

			[JsonPropertyName("name")]
			public string Name { get; set; } = "";
		}

		private class DeezerTracksResponse
		{
			[JsonPropertyName("data")]
			public List<DeezerTrack>? Data { get; set; }
		}
	}

	public class DeezerTrack
	{
		[JsonPropertyName("id")]
		public long Id { get; set; }

		[JsonPropertyName("title")]
		public string Title { get; set; } = "";

		[JsonPropertyName("preview")]
		public string? Preview { get; set; }

		[JsonPropertyName("duration")]
		public int Duration { get; set; }

		[JsonPropertyName("album")]
		public DeezerAlbum? Album { get; set; }
	}

	public class DeezerAlbum
	{
		[JsonPropertyName("title")]
		public string Title { get; set; } = "";

		[JsonPropertyName("cover_medium")]
		public string? CoverMedium { get; set; }

		[JsonPropertyName("cover_big")]
		public string? CoverBig { get; set; }
	}
}