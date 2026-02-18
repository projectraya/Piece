using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Piece.Data;
using Piece.Services;
using Piece.Data.Models;
using Piece.Data.Enums;

namespace Piece.Tests.Services
{
	[TestFixture]
	public class FavoriteServiceTests
	{
		private IDbContextFactory<ApplicationDbContext> _dbFactory;
		private FavoriteService _service;

		[SetUp]
		public void Setup()
		{
			var options = new DbContextOptionsBuilder<ApplicationDbContext>()
				.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
				.Options;

			var factory = new TestDbContextFactory(options);
			_dbFactory = factory;
			_service = new FavoriteService(_dbFactory);
		}

		[Test]
		public async Task ToggleFavoriteAsync_AddsToFavorites()
		{
			// Arrange
			var userId = "user1";
			var track = await CreateTestTrackAsync();

			// Act
			var result = await _service.ToggleFavoriteAsync(userId, track.Id);

			// Assert
			Assert.That(result, Is.True); 
			var isFavorite = await _service.IsFavoriteAsync(userId, track.Id);
			Assert.That(isFavorite, Is.True);
		}

		[Test]
		public async Task ToggleFavoriteAsync_RemovesFromFavorites()
		{
			// Arrange
			var userId = "user1";
			var track = await CreateTestTrackAsync();
			await _service.ToggleFavoriteAsync(userId, track.Id);

			// Act
			var result = await _service.ToggleFavoriteAsync(userId, track.Id);

			// Assert
			Assert.That(result, Is.False); 
			var isFavorite = await _service.IsFavoriteAsync(userId, track.Id);
			Assert.That(isFavorite, Is.False);
		}

		[Test]
		public async Task GetUserFavoritesAsync_ReturnsAllFavorites()
		{
			// Arrange
			var userId = "user1";
			var track1 = await CreateTestTrackAsync("Track 1");
			var track2 = await CreateTestTrackAsync("Track 2");

			await _service.ToggleFavoriteAsync(userId, track1.Id);
			await _service.ToggleFavoriteAsync(userId, track2.Id);

			// Act
			var favorites = await _service.GetUserFavoritesAsync(userId);

			// Assert
			Assert.That(favorites.Count, Is.EqualTo(2));
		}

		[Test]
		public async Task GetUserFavoriteTrackIdsAsync_ReturnsIds()
		{
			// Arrange
			var userId = "user1";
			var track1 = await CreateTestTrackAsync();
			var track2 = await CreateTestTrackAsync();

			await _service.ToggleFavoriteAsync(userId, track1.Id);
			await _service.ToggleFavoriteAsync(userId, track2.Id);

			// Act
			var ids = await _service.GetUserFavoriteTrackIdsAsync(userId);

			// Assert
			Assert.That(ids.Count, Is.EqualTo(2));
			Assert.That(ids, Does.Contain(track1.Id));
			Assert.That(ids, Does.Contain(track2.Id));
		}

		[Test]
		public async Task ToggleExternalFavoriteAsync_AddsExternalFavorite()
		{
			// Arrange
			var userId = "user1";
			var source = TrackSource.Jamendo;
			var externalId = "ext123";

			// Act
			var result = await _service.ToggleExternalFavoriteAsync(
				userId,
				source,
				externalId,
				"Test Track",
				"Test Artist",
				"http://audio.url",
				"http://image.url"
			);

			// Assert
			Assert.That(result, Is.True);
			var isFavorite = await _service.IsExternalFavoriteAsync(userId, source, externalId);
			Assert.That(isFavorite, Is.True);
		}

		[Test]
		public async Task GetUserExternalFavoritesAsync_ReturnsExternalFavorites()
		{
			// Arrange
			var userId = "user1";
			await _service.ToggleExternalFavoriteAsync(
				userId,
				TrackSource.Jamendo,
				"ext1",
				"Track 1",
				"Artist 1",
				"url1",
				null
			);
			await _service.ToggleExternalFavoriteAsync(
				userId,
				TrackSource.Jamendo,
				"ext2",
				"Track 2",
				"Artist 2",
				"url2",
				null
			);

			// Act
			var favorites = await _service.GetUserExternalFavoritesAsync(userId);

			// Assert
			Assert.That(favorites.Count, Is.EqualTo(2));
		}

		[Test]
		public async Task GetUserExternalFavoritesAsync_FiltersBySource()
		{
			// Arrange
			var userId = "user1";
			await _service.ToggleExternalFavoriteAsync(
				userId, TrackSource.Jamendo, "ext1", "Track", "Artist", "url", null
			);
			await _service.ToggleExternalFavoriteAsync(
				userId, TrackSource.Local, "ext2", "Track", "Artist", "url", null
			);

			// Act
			var jamendoFavorites = await _service.GetUserExternalFavoritesAsync(userId, TrackSource.Jamendo);

			// Assert
			Assert.That(jamendoFavorites.Count, Is.EqualTo(1));
			Assert.That(jamendoFavorites[0].Source, Is.EqualTo(TrackSource.Jamendo));
		}

		// Helper
		private async Task<Track> CreateTestTrackAsync(string title = "Test Track")
		{
			using var context = await _dbFactory.CreateDbContextAsync();
			var track = new Track
			{
				Title = title,
				ArtistName = "Test Artist",
				DurationSeconds = 180,
				LocalFilePath = "/test.mp3",
				Source = TrackSource.Local,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			context.Tracks.Add(track);
			await context.SaveChangesAsync();
			return track;
		}

		[TearDown]
		public void TearDown()
		{
			_dbFactory = null;
		}
	}
}