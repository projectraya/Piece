using NUnit.Framework;
using Moq;
using Moq.Contrib.HttpClient;
using Microsoft.EntityFrameworkCore;
using Piece.Services;
using Piece.Data;
using Piece.Data.Models;
using Piece.Data.Enums;
using System.Net;

namespace Piece.Tests.Services
{
	[TestFixture]
	public class MusicBrainzServiceTests
	{
		private Mock<HttpMessageHandler> _httpMessageHandler;
		private HttpClient _httpClient;
		private IDbContextFactory<ApplicationDbContext> _dbFactory;
		private MusicBrainzService _service;

		[SetUp]
		public void Setup()
		{
			_httpMessageHandler = new Mock<HttpMessageHandler>();
			_httpClient = _httpMessageHandler.CreateClient();

			var options = new DbContextOptionsBuilder<ApplicationDbContext>()
				.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
				.Options;

			_dbFactory = new TestDbContextFactory(options);
			_service = new MusicBrainzService(_httpClient, _dbFactory);
		}

		[Test]
		public async Task SearchArtistsByCountry_ReturnsArtists()
		{
			// Arrange
			var jsonResponse = @"{
                ""artists"": [
                    {
                        ""id"": ""artist-1"",
                        ""name"": ""Bulgarian Artist"",
                        ""country"": ""BG"",
                        ""type"": ""Person"",
                        ""disambiguation"": ""Famous singer"",
                        ""area"": {
                            ""name"": ""Bulgaria""
                        }
                    },
                    {
                        ""id"": ""artist-2"",
                        ""name"": ""Another Artist"",
                        ""type"": ""Group"",
                        ""begin-area"": {
                            ""name"": ""Sofia""
                        }
                    }
                ]
            }";

			_httpMessageHandler
				.SetupRequest(HttpMethod.Get, req => req.RequestUri!.ToString().Contains("Bulgaria"))
				.ReturnsResponse(jsonResponse, "application/json");

			// Act
			var artists = await _service.SearchArtistsByCountry("Bulgaria", 100);

			// Assert
			Assert.That(artists.Count, Is.EqualTo(2));
			Assert.That(artists[0].Name, Is.EqualTo("Bulgarian Artist"));
			Assert.That(artists[0].MusicBrainzId, Is.EqualTo("artist-1"));
			Assert.That(artists[0].Country, Is.EqualTo("BG"));
			Assert.That(artists[0].Genre, Is.EqualTo("Person"));
			Assert.That(artists[0].Bio, Is.EqualTo("Famous singer"));
		}

		[Test]
		public async Task SearchArtistsByCountry_HandlesEmptyResponse()
		{
			// Arrange
			var jsonResponse = @"{""artists"": []}";

			_httpMessageHandler.SetupAnyRequest().ReturnsResponse(jsonResponse, "application/json");

			// Act
			var artists = await _service.SearchArtistsByCountry("NonExistentCountry", 100);

			// Assert
			Assert.That(artists.Count, Is.EqualTo(0));
		}

		[Test]
		public async Task SearchArtistsByCountry_EscapesCountryName()
		{
			// Arrange
			var jsonResponse = @"{""artists"": []}";

			_httpMessageHandler
				.SetupRequest(HttpMethod.Get, req => req.RequestUri!.ToString().Contains("United%20States"))
				.ReturnsResponse(jsonResponse, "application/json");

			// Act
			await _service.SearchArtistsByCountry("United States", 100);

			// Assert
			_httpMessageHandler.VerifyAnyRequest(Times.Once());
		}

		[Test]
		public async Task SearchArtistsByCountry_HandlesAPIError_ReturnsEmpty()
		{
			// Arrange
			_httpMessageHandler.SetupAnyRequest().ReturnsResponse(HttpStatusCode.InternalServerError);

			// Act
			var artists = await _service.SearchArtistsByCountry("Bulgaria", 100);

			// Assert
			Assert.That(artists.Count, Is.EqualTo(0));
		}

		[Test]
		public async Task SearchArtistsByCountry_SetsUserAgentHeader()
		{
			// Arrange
			var jsonResponse = @"{""artists"": []}";

			_httpMessageHandler.SetupAnyRequest().ReturnsResponse(jsonResponse, "application/json");

			// Act
			await _service.SearchArtistsByCountry("Bulgaria", 100);

			// Assert
			Assert.That(_httpClient.DefaultRequestHeaders.UserAgent.ToString(),
				Does.Contain("Piece/1.0"));
		}

		[Test]
		public async Task SearchArtistsByCountry_HandlesNullFields()
		{
			// Arrange
			var jsonResponse = @"{
                ""artists"": [
                    {
                        ""id"": ""artist-3"",
                        ""name"": null,
                        ""country"": null,
                        ""type"": null,
                        ""disambiguation"": null
                    }
                ]
            }";

			_httpMessageHandler.SetupAnyRequest().ReturnsResponse(jsonResponse, "application/json");

			// Act
			var artists = await _service.SearchArtistsByCountry("Test", 100);

			// Assert
			Assert.That(artists.Count, Is.EqualTo(1));
			Assert.That(artists[0].Name, Is.EqualTo("Unknown Artist"));
			Assert.That(artists[0].Genre, Is.EqualTo("Unknown"));
		}

		[Test]
		public async Task SeedArtistsForCountry_AddsNewArtists()
		{
			// Arrange
			var countryId = 1;
			var jsonResponse = @"{
                ""artists"": [{
                    ""id"": ""new-artist"",
                    ""name"": ""New Artist"",
                    ""type"": ""Person""
                }]
            }";

			_httpMessageHandler.SetupAnyRequest().ReturnsResponse(jsonResponse, "application/json");

			// Act
			await _service.SeedArtistsForCountry(countryId, "Bulgaria", 30);

			// Assert
			using var context = await _dbFactory.CreateDbContextAsync();
			var artists = await context.Artists.ToListAsync();
			Assert.That(artists.Count, Is.EqualTo(1));
			Assert.That(artists[0].Name, Is.EqualTo("New Artist"));
			Assert.That(artists[0].CountryId, Is.EqualTo(countryId));
			Assert.That(artists[0].DataSource, Is.EqualTo(ArtistDataSource.MusicBrainz));
		}

		[Test]
		public async Task SeedArtistsForCountry_SkipsDuplicates()
		{
			// Arrange
			var countryId = 1;

			var context = await _dbFactory.CreateDbContextAsync();
			using (context)
			{
				context.Artists.Add(new Artist
				{
					MusicBrainzId = "existing-artist",
					Name = "Existing",
					CountryId = countryId
				});
				await context.SaveChangesAsync();
			}

			var jsonResponse = @"{
                ""artists"": [{
                    ""id"": ""existing-artist"",
                    ""name"": ""Existing"",
                    ""type"": ""Person""
                }]
            }";

			_httpMessageHandler.SetupAnyRequest().ReturnsResponse(jsonResponse, "application/json");

			// Act
			await _service.SeedArtistsForCountry(countryId, "Bulgaria", 30);

			// Assert
			context = await _dbFactory.CreateDbContextAsync();
			var artists = await context.Artists.ToListAsync();
			Assert.That(artists.Count, Is.EqualTo(1));
		}

		[TearDown]
		public void TearDown()
		{
			_httpClient?.Dispose();
		}
	}
}