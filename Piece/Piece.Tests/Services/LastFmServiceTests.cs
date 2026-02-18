using NUnit.Framework;
using Moq;
using Moq.Contrib.HttpClient;
using Microsoft.Extensions.Configuration;
using Piece.Services.LastFm;
using System.Net;

namespace Piece.Tests.Services
{
	[TestFixture]
	public class LastFmServiceTests
	{
		private Mock<HttpMessageHandler> _httpMessageHandler;
		private HttpClient _httpClient;
		private Mock<IConfiguration> _mockConfiguration;
		private LastFmService _service;

		[SetUp]
		public void Setup()
		{
			_httpMessageHandler = new Mock<HttpMessageHandler>();
			_httpClient = _httpMessageHandler.CreateClient();

			_mockConfiguration = new Mock<IConfiguration>();
			_mockConfiguration.Setup(c => c["LastFm:ApiKey"]).Returns("test-api-key");

			_service = new LastFmService(_httpClient, _mockConfiguration.Object);
		}

		[Test]
		public async Task GetTopArtistsAsync_ReturnsArtistNames()
		{
			// Arrange
			var jsonResponse = @"{
                ""topartists"": {
                    ""artist"": [
                        {""name"": ""Artist 1""},
                        {""name"": ""Artist 2""},
                        {""name"": ""Artist 3""}
                    ]
                }
            }";

			_httpMessageHandler
				.SetupRequest(HttpMethod.Get, req =>
					req.RequestUri!.ToString().Contains("geo.gettopartists"))
				.ReturnsResponse(jsonResponse, "application/json");

			// Act
			var artists = await _service.GetTopArtistsAsync("US", 30);

			// Assert
			Assert.That(artists.Count, Is.EqualTo(3));
			Assert.That(artists[0], Is.EqualTo("Artist 1"));
			Assert.That(artists[1], Is.EqualTo("Artist 2"));
			Assert.That(artists[2], Is.EqualTo("Artist 3"));
		}

		[Test]
		public async Task GetTopArtistsAsync_WithEmptyResponse_ReturnsEmpty()
		{
			// Arrange
			var jsonResponse = @"{""topartists"": null}";

			_httpMessageHandler.SetupAnyRequest().ReturnsResponse(jsonResponse, "application/json");

			// Act
			var artists = await _service.GetTopArtistsAsync("US", 30);

			// Assert
			Assert.That(artists.Count, Is.EqualTo(0));
		}

		[Test]
		public async Task GetTopArtistsAsync_IncludesApiKeyInRequest()
		{
			// Arrange
			var jsonResponse = @"{""topartists"": {""artist"": []}}";

			_httpMessageHandler
				.SetupRequest(HttpMethod.Get, req =>
					req.RequestUri!.ToString().Contains("api_key=test-api-key"))
				.ReturnsResponse(jsonResponse, "application/json");

			// Act
			await _service.GetTopArtistsAsync("US", 30);

			// Assert
			_httpMessageHandler.VerifyAnyRequest(Times.Once());
		}

		[Test]
		public async Task GetTopTracksAsync_ReturnsTracksList()
		{
			// Arrange
			var jsonResponse = @"{
                ""toptracks"": {
                    ""track"": [
                        {
                            ""name"": ""Track 1"",
                            ""artist"": {""name"": ""Artist 1""}
                        },
                        {
                            ""name"": ""Track 2"",
                            ""artist"": {""name"": ""Artist 2""}
                        }
                    ]
                }
            }";

			_httpMessageHandler
				.SetupRequest(HttpMethod.Get, req =>
					req.RequestUri!.ToString().Contains("geo.gettoptracks"))
				.ReturnsResponse(jsonResponse, "application/json");

			// Act
			var tracks = await _service.GetTopTracksAsync("UK", 30);

			// Assert
			Assert.That(tracks.Count, Is.EqualTo(2));
			// Note: Actual Track properties depend on LastFmTrack class definition
		}

		[Test]
		public async Task GetTopTracksAsync_WithNullResponse_ReturnsEmpty()
		{
			// Arrange
			var jsonResponse = @"{""toptracks"": null}";

			_httpMessageHandler.SetupAnyRequest().ReturnsResponse(jsonResponse, "application/json");

			// Act
			var tracks = await _service.GetTopTracksAsync("UK", 30);

			// Assert
			Assert.That(tracks.Count, Is.EqualTo(0));
		}

		[Test]
		public async Task GetTopArtistsAsync_RespectsLimit()
		{
			// Arrange
			var jsonResponse = @"{""topartists"": {""artist"": []}}";

			_httpMessageHandler
				.SetupRequest(HttpMethod.Get, req =>
					req.RequestUri!.ToString().Contains("limit=50"))
				.ReturnsResponse(jsonResponse, "application/json");

			// Act
			await _service.GetTopArtistsAsync("US", 50);

			// Assert
			_httpMessageHandler.VerifyRequest(HttpMethod.Get,
				req => req.RequestUri!.ToString().Contains("limit=50"), Times.Once());
		}

		[Test]
		public async Task GetTopArtistsAsync_WhenAPIFails_ThrowsException()
		{
			// Arrange
			_httpMessageHandler
				.SetupAnyRequest()
				.ReturnsResponse(HttpStatusCode.InternalServerError);

			// Act & Assert
			Assert.ThrowsAsync<HttpRequestException>(async () =>
				await _service.GetTopArtistsAsync("US", 30));
		}

		[TearDown]
		public void TearDown()
		{
			_httpClient?.Dispose();
		}
	}
}