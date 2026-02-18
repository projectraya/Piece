using NUnit.Framework;
using Moq;
using Moq.Contrib.HttpClient;
using Piece.Services;
using System.Net;

namespace Piece.Tests.Services
{
	[TestFixture]
	public class DeezerServiceTests
	{
		private Mock<HttpMessageHandler> _httpMessageHandler;
		private HttpClient _httpClient;
		private DeezerService _service;

		[SetUp]
		public void Setup()
		{
			_httpMessageHandler = new Mock<HttpMessageHandler>();
			_httpClient = _httpMessageHandler.CreateClient();
			_httpClient.BaseAddress = new Uri("https://api.deezer.com/");
			_service = new DeezerService(_httpClient);
		}

		[Test]
		public async Task GetArtistTopTracksAsync_ReturnsTracksWithPreviews()
		{
			// Arrange
			var artistSearchResponse = @"{
                ""data"": [{
                    ""id"": 12345,
                    ""name"": ""Test Artist""
                }]
            }";

			// Mock top tracks response
			var tracksResponse = @"{
                ""data"": [
                    {
                        ""id"": 1,
                        ""title"": ""Track 1"",
                        ""preview"": ""https://preview1.mp3"",
                        ""duration"": 180,
                        ""album"": {
                            ""title"": ""Album 1"",
                            ""cover_medium"": ""https://cover1.jpg"",
                            ""cover_big"": ""https://cover1-big.jpg""
                        }
                    },
                    {
                        ""id"": 2,
                        ""title"": ""Track 2"",
                        ""preview"": ""https://preview2.mp3"",
                        ""duration"": 240,
                        ""album"": {
                            ""title"": ""Album 2"",
                            ""cover_medium"": ""https://cover2.jpg""
                        }
                    },
                    {
                        ""id"": 3,
                        ""title"": ""Track Without Preview"",
                        ""preview"": null,
                        ""duration"": 200
                    }
                ]
            }";

			_httpMessageHandler
				.SetupRequest(HttpMethod.Get, req => req.RequestUri!.ToString().Contains("search/artist"))
				.ReturnsResponse(artistSearchResponse, "application/json");

			_httpMessageHandler
				.SetupRequest(HttpMethod.Get, req => req.RequestUri!.ToString().Contains("artist/12345/top"))
				.ReturnsResponse(tracksResponse, "application/json");

			// Act
			var tracks = await _service.GetArtistTopTracksAsync("Test Artist", 3);

			// Assert
			Assert.That(tracks.Count, Is.EqualTo(2));
			Assert.That(tracks[0].Title, Is.EqualTo("Track 1"));
			Assert.That(tracks[0].Preview, Is.EqualTo("https://preview1.mp3"));
			Assert.That(tracks[0].Duration, Is.EqualTo(180));
			Assert.That(tracks[0].Album!.Title, Is.EqualTo("Album 1"));
		}

		[Test]
		public async Task GetArtistTopTracksAsync_WhenArtistNotFound_ReturnsEmpty()
		{
			// Arrange
			var artistSearchResponse = @"{""data"": []}";

			_httpMessageHandler
				.SetupAnyRequest()
				.ReturnsResponse(artistSearchResponse, "application/json");

			// Act
			var tracks = await _service.GetArtistTopTracksAsync("Unknown Artist", 3);

			// Assert
			Assert.That(tracks.Count, Is.EqualTo(0));
		}

		[Test]
		public async Task GetArtistTopTracksAsync_WhenNoTracksFound_ReturnsEmpty()
		{
			// Arrange
			var artistSearchResponse = @"{
                ""data"": [{""id"": 12345, ""name"": ""Test Artist""}]
            }";

			var tracksResponse = @"{""data"": null}";

			_httpMessageHandler
				.SetupRequest(HttpMethod.Get, req => req.RequestUri!.ToString().Contains("search/artist"))
				.ReturnsResponse(artistSearchResponse, "application/json");

			_httpMessageHandler
				.SetupRequest(HttpMethod.Get, req => req.RequestUri!.ToString().Contains("artist/"))
				.ReturnsResponse(tracksResponse, "application/json");

			// Act
			var tracks = await _service.GetArtistTopTracksAsync("Test Artist", 3);

			// Assert
			Assert.That(tracks.Count, Is.EqualTo(0));
		}

		[Test]
		public async Task GetArtistTopTracksAsync_FiltersOutTracksWithoutPreviews()
		{
			// Arrange
			var artistSearchResponse = @"{
                ""data"": [{""id"": 999, ""name"": ""Artist""}]
            }";

			var tracksResponse = @"{
                ""data"": [
                    {""id"": 1, ""title"": ""Track 1"", ""preview"": ""url1"", ""duration"": 100},
                    {""id"": 2, ""title"": ""Track 2"", ""preview"": null, ""duration"": 100},
                    {""id"": 3, ""title"": ""Track 3"", ""preview"": """", ""duration"": 100}
                ]
            }";

			_httpMessageHandler
				.SetupRequest(HttpMethod.Get, req => req.RequestUri!.ToString().Contains("search"))
				.ReturnsResponse(artistSearchResponse, "application/json");

			_httpMessageHandler
				.SetupRequest(HttpMethod.Get, req => req.RequestUri!.ToString().Contains("top"))
				.ReturnsResponse(tracksResponse, "application/json");

			// Act
			var tracks = await _service.GetArtistTopTracksAsync("Artist", 3);

			// Assert
			Assert.That(tracks.Count, Is.EqualTo(1));
			Assert.That(tracks.All(t => !string.IsNullOrEmpty(t.Preview)), Is.True);
		}

		[Test]
		public async Task GetArtistTopTracksAsync_EscapesArtistName()
		{
			// Arrange
			var artistSearchResponse = @"{""data"": []}";

			_httpMessageHandler
				.SetupRequest(HttpMethod.Get, req =>
					req.RequestUri!.ToString().Contains("Special%20%26%20Characters"))
				.ReturnsResponse(artistSearchResponse, "application/json");

			// Act
			await _service.GetArtistTopTracksAsync("Special & Characters", 3);

			// Assert
			_httpMessageHandler.VerifyAnyRequest(Times.Once());
		}

		[Test]
		public async Task GetArtistTopTracksAsync_WhenAPIFails_ReturnsEmpty()
		{
			// Arrange
			_httpMessageHandler
				.SetupAnyRequest()
				.ReturnsResponse(HttpStatusCode.InternalServerError);

			// Act
			var tracks = await _service.GetArtistTopTracksAsync("Artist", 3);

			// Assert
			Assert.That(tracks.Count, Is.EqualTo(0));
		}

		[Test]
		public async Task GetArtistTopTracksAsync_RespectsLimit()
		{
			// Arrange
			var artistSearchResponse = @"{
                ""data"": [{""id"": 111, ""name"": ""Artist""}]
            }";

			_httpMessageHandler
				.SetupRequest(HttpMethod.Get, req => req.RequestUri!.ToString().Contains("search"))
				.ReturnsResponse(artistSearchResponse, "application/json");

			_httpMessageHandler
				.SetupRequest(HttpMethod.Get, req => req.RequestUri!.ToString().Contains("limit=5"))
				.ReturnsResponse(@"{""data"": []}", "application/json");

			// Act
			await _service.GetArtistTopTracksAsync("Artist", 5);

			// Assert
			_httpMessageHandler.VerifyRequest(HttpMethod.Get,
				req => req.RequestUri!.ToString().Contains("limit=5"), Times.Once());
		}

		[TearDown]
		public void TearDown()
		{
			_httpClient?.Dispose();
		}
	}
}