using NUnit.Framework;
using Moq;
using Moq.Contrib.HttpClient;
using Piece.Services;
using Piece.Data.Enums;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Json;

namespace Piece.Tests.Services
{
	[TestFixture]
	public class JamendoServiceTests
	{
		private Mock<HttpMessageHandler> _httpMessageHandler;
		private HttpClient _httpClient;
		private Mock<IConfiguration> _mockConfiguration;
		private JamendoService _service;

		[SetUp]
		public void Setup()
		{
			_httpMessageHandler = new Mock<HttpMessageHandler>();
			_httpClient = _httpMessageHandler.CreateClient();
			_httpClient.BaseAddress = new Uri("https://api.jamendo.com/v3.0/");

			_mockConfiguration = new Mock<IConfiguration>();
			_mockConfiguration.Setup(c => c["Jamendo:ClientId"]).Returns("test-client-id");

			_service = new JamendoService(_httpClient, _mockConfiguration.Object);
		}

		[Test]
		public async Task SearchTracksAsync_ReturnsTracksFromAPI()
		{
			// Arrange
			var jsonResponse = @"{
                ""results"": [
                    {
                        ""id"": ""123"",
                        ""name"": ""Test Track"",
                        ""artist_name"": ""Test Artist"",
                        ""album_name"": ""Test Album"",
                        ""duration"": 180,
                        ""album_image"": ""https://example.com/image.jpg""
                    },
                    {
                        ""id"": ""456"",
                        ""name"": ""Another Track"",
                        ""artist_name"": ""Another Artist"",
                        ""album_name"": ""Another Album"",
                        ""duration"": 240,
                        ""album_image"": ""https://example.com/image2.jpg""
                    }
                ]
            }";

			_httpMessageHandler
				.SetupRequest(HttpMethod.Get, "https://api.jamendo.com/v3.0/tracks/?client_id=test-client-id&format=json&limit=20&search=rock")
				.ReturnsResponse(jsonResponse, "application/json");

			// Act
			var tracks = await _service.SearchTracksAsync("rock", 20);

			// Assert
			Assert.That(tracks.Count, Is.EqualTo(2));
			Assert.That(tracks[0].Title, Is.EqualTo("Test Track"));
			Assert.That(tracks[0].ArtistName, Is.EqualTo("Test Artist"));
			Assert.That(tracks[0].Source, Is.EqualTo(TrackSource.Jamendo));
			Assert.That(tracks[0].JamendoTrackId, Is.EqualTo("123"));
			Assert.That(tracks[0].DurationSeconds, Is.EqualTo(180));
		}

		[Test]
		public async Task SearchTracksAsync_WithEmptyResults_ReturnsEmptyList()
		{
			// Arrange
			var jsonResponse = @"{""results"": []}";

			_httpMessageHandler
				.SetupAnyRequest()
				.ReturnsResponse(jsonResponse, "application/json");

			// Act
			var tracks = await _service.SearchTracksAsync("nonexistent", 20);

			// Assert
			Assert.That(tracks.Count, Is.EqualTo(0));
		}

		[Test]
		public async Task SearchTracksAsync_WhenAPIFails_ReturnsEmptyList()
		{
			// Arrange
			_httpMessageHandler
				.SetupAnyRequest()
				.ReturnsResponse(HttpStatusCode.InternalServerError);

			// Act
			var tracks = await _service.SearchTracksAsync("test", 20);

			// Assert
			Assert.That(tracks.Count, Is.EqualTo(0));
		}

		[Test]
		public async Task SearchTracksAsync_EscapesQueryString()
		{
			// Arrange
			var jsonResponse = @"{""results"": []}";

			_httpMessageHandler
				.SetupRequest(HttpMethod.Get, req => req.RequestUri!.ToString().Contains("special%20chars"))
				.ReturnsResponse(jsonResponse, "application/json");

			// Act
			await _service.SearchTracksAsync("special chars", 20);

			// Assert 
			_httpMessageHandler.VerifyAnyRequest(Times.Once());
		}

		[Test]
		public void GetStreamUrl_ReturnsCorrectUrl()
		{
			// Arrange
			var trackId = "123456";

			// Act
			var url = _service.GetStreamUrl(trackId);

			// Assert
			Assert.That(url, Is.EqualTo("https://mp3l.jamendo.com/?trackid=123456&format=mp31"));
		}

		[Test]
		public async Task SearchTracksAsync_SetsCorrectTrackProperties()
		{
			// Arrange
			var jsonResponse = @"{
                ""results"": [{
                    ""id"": ""789"",
                    ""name"": ""Property Test"",
                    ""artist_name"": ""Artist"",
                    ""album_name"": ""Album"",
                    ""duration"": 300,
                    ""album_image"": ""https://img.example.com""
                }]
            }";

			_httpMessageHandler.SetupAnyRequest().ReturnsResponse(jsonResponse, "application/json");

			// Act
			var tracks = await _service.SearchTracksAsync("test");

			// Assert
			var track = tracks[0];
			Assert.That(track.IsActive, Is.True);
			Assert.That(track.Source, Is.EqualTo(TrackSource.Jamendo));
			Assert.That(track.CoverImageUrl, Is.EqualTo("https://img.example.com"));
			Assert.That(track.AlbumName, Is.EqualTo("Album"));
		}

		[TearDown]
		public void TearDown()
		{
			_httpClient?.Dispose();
		}
	}
}