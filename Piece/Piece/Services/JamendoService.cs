using Piece.Data.Enums;
using Piece.Data.Models;
using System.Text.Json;

namespace Piece.Services
{
	public class JamendoService
	{
		private readonly HttpClient _httpClient;
		private readonly string _clientId;

		public JamendoService(HttpClient httpClient, IConfiguration configuration)
		{
			_httpClient = httpClient;
			_clientId = configuration["Jamendo:ClientId"]
				?? throw new InvalidOperationException("Jamendo ClientId not configured in User Secrets");
			_httpClient.BaseAddress = new Uri("https://api.jamendo.com/v3.0/");
		}

		public async Task<List<Track>> SearchTracksAsync(string query, int limit = 20)
		{
			try
			{
				var url = $"tracks/?client_id={_clientId}&format=json&limit={limit}&search={Uri.EscapeDataString(query)}";
				var response = await _httpClient.GetAsync(url);
				response.EnsureSuccessStatusCode();

				var json = await response.Content.ReadAsStringAsync();
				var result = JsonSerializer.Deserialize<JamendoResponse>(json, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

				return result?.Results?.Select(r => new Track
				{
					Title = r.Name,
					ArtistName = r.Artist_Name,
					AlbumName = r.Album_Name,
					DurationSeconds = r.Duration,
					Source = TrackSource.Jamendo,
					JamendoTrackId = r.Id,
					CoverImageUrl = r.Album_Image,
					YearPublished = null,
					GenreId = null,
					IsActive = true
				}).ToList() ?? new List<Track>();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Jamendo API Error: {ex.Message}");
				return new List<Track>();
			}
		}

		public string GetStreamUrl(string jamendoTrackId)
		{
			return $"https://mp3l.jamendo.com/?trackid={jamendoTrackId}&format=mp31";
		}

		// Response models
		private class JamendoResponse
		{
			public List<JamendoTrack>? Results { get; set; }
		}

		private class JamendoTrack
		{
			public string Id { get; set; } = string.Empty;
			public string Name { get; set; } = string.Empty;
			public string Artist_Name { get; set; } = string.Empty;
			public string Album_Name { get; set; } = string.Empty;
			public int Duration { get; set; }
			public string Album_Image { get; set; } = string.Empty;
		}
	}
}
