using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace Piece.Services.LastFm
{
	public class LastFmService
	{
		private readonly HttpClient _http;
		private readonly string _apiKey;

		public LastFmService(HttpClient http, IConfiguration config)
		{
			_http = http;
			_apiKey = config["LastFm:ApiKey"]!;
		}

		public async Task<List<string>> GetTopArtistsAsync(string country, int limit = 30)
		{
			var url =
				$"https://ws.audioscrobbler.com/2.0/?method=geo.gettopartists" +
				$"&country={country}&limit={limit}&api_key={_apiKey}&format=json";

			var result = await _http.GetFromJsonAsync<LastFmTopArtistsResponse>(url);
			return result?.TopArtists?.Artist.Select(a => a.Name).ToList() ?? new();
		}

		public async Task<List<LastFmTrack>> GetTopTracksAsync(string country, int limit = 30)
		{
			var url =
				$"https://ws.audioscrobbler.com/2.0/?method=geo.gettoptracks" +
				$"&country={country}&limit={limit}&api_key={_apiKey}&format=json";

			var result = await _http.GetFromJsonAsync<LastFmTopTracksResponse>(url);
			return result?.TopTracks?.Track ?? new();
		}
	}
}
