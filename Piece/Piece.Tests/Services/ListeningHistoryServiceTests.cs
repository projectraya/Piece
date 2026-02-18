using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Piece.Data;
using Piece.Services;
using Piece.Data.Models;
using Piece.Data.Enums;

namespace Piece.Tests.Services
{
	[TestFixture]
	public class ListeningHistoryServiceTests
	{
		private IDbContextFactory<ApplicationDbContext> _dbFactory;
		private ListeningHistoryService _service;

		[SetUp]
		public void Setup()
		{
			var options = new DbContextOptionsBuilder<ApplicationDbContext>()
				.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
				.Options;

			var factory = new TestDbContextFactory(options);
			_dbFactory = factory;
			_service = new ListeningHistoryService(_dbFactory);
		}

		[Test]
		public async Task RecordListeningAsync_CreatesHistoryRecord()
		{
			// Arrange
			var userId = "user1";
			var track = await CreateTestTrackAsync();
			var duration = 180;

			// Act
			await _service.RecordListeningAsync(userId, track.Id, duration);

			// Assert
			using var context = await _dbFactory.CreateDbContextAsync();
			var history = await context.ListeningHistory
				.FirstOrDefaultAsync(h => h.UserId == userId && h.TrackId == track.Id);

			Assert.That(history, Is.Not.Null);
			Assert.That(history.DurationListened, Is.EqualTo(duration));
		}

		[Test]
		public async Task GetTopGenresAsync_ReturnsGenresOrderedByCount()
		{
			// Arrange
			var userId = "user1";
			var genre1 = await CreateTestGenreAsync("Rock", "#FF0000");
			var genre2 = await CreateTestGenreAsync("Pop", "#00FF00");

			var track1 = await CreateTestTrackAsync("Track 1", genreId: genre1.Id);
			var track2 = await CreateTestTrackAsync("Track 2", genreId: genre2.Id);

			await _service.RecordListeningAsync(userId, track1.Id, 180);
			await _service.RecordListeningAsync(userId, track1.Id, 180);
			await _service.RecordListeningAsync(userId, track1.Id, 180);
			await _service.RecordListeningAsync(userId, track2.Id, 180);

			// Act
			var topGenres = await _service.GetTopGenresAsync(userId, days: 30);

			// Assert
			Assert.That(topGenres.Count, Is.EqualTo(2));
			var genreList = topGenres.ToList();
			Assert.That(genreList[0].Key, Is.EqualTo("Rock"));
			Assert.That(genreList[0].Value.Count, Is.EqualTo(3));
			Assert.That(genreList[1].Key, Is.EqualTo("Pop"));
			Assert.That(genreList[1].Value.Count, Is.EqualTo(1));
		}

		[Test]
		public async Task GetTopGenresAsync_ReturnsCorrectColors()
		{
			// Arrange
			var userId = "user1";
			var genre = await CreateTestGenreAsync("Electronic", "#667eea");
			var track = await CreateTestTrackAsync(genreId: genre.Id);

			await _service.RecordListeningAsync(userId, track.Id, 180);

			// Act
			var topGenres = await _service.GetTopGenresAsync(userId);

			// Assert
			Assert.That(topGenres["Electronic"].Color, Is.EqualTo("#667eea"));
		}

		[Test]
		public async Task GetTopGenresAsync_WithNoHistory_ReturnsEmpty()
		{
			// Arrange
			var userId = "user1";

			// Act
			var topGenres = await _service.GetTopGenresAsync(userId);

			// Assert
			Assert.That(topGenres.Count, Is.EqualTo(0));
		}

		[Test]
		public async Task GetGenreHistoryAsync_ReturnsDataGroupedByDate()
		{
			// Arrange
			var userId = "user1";
			var genre = await CreateTestGenreAsync("Rock", "#FF0000");
			var track = await CreateTestTrackAsync(genreId: genre.Id);

			var today = DateTime.UtcNow.Date;
			var yesterday = today.AddDays(-1);

			await CreateHistoryRecordAsync(userId, track.Id, today);
			await CreateHistoryRecordAsync(userId, track.Id, today);
			await CreateHistoryRecordAsync(userId, track.Id, yesterday);

			// Act
			var history = await _service.GetGenreHistoryAsync(userId, yesterday, today);

			// Assert
			Assert.That(history.Count, Is.EqualTo(2)); 
			Assert.That(history[today].Count, Is.EqualTo(1)); 
			Assert.That(history[today][0].Count, Is.EqualTo(2)); 
			Assert.That(history[yesterday][0].Count, Is.EqualTo(1)); 
		}

		[Test]
		public async Task GetGenreHistoryAsync_FiltersDateRange()
		{
			// Arrange
			var userId = "user1";
			var genre = await CreateTestGenreAsync("Pop");
			var track = await CreateTestTrackAsync(genreId: genre.Id);

			var inRange = DateTime.UtcNow.Date.AddDays(-5);
			var outOfRange = DateTime.UtcNow.Date.AddDays(-15);

			await CreateHistoryRecordAsync(userId, track.Id, inRange);
			await CreateHistoryRecordAsync(userId, track.Id, outOfRange);

			// Act
			var startDate = DateTime.UtcNow.Date.AddDays(-10);
			var endDate = DateTime.UtcNow.Date;
			var history = await _service.GetGenreHistoryAsync(userId, startDate, endDate);

			// Assert
			Assert.That(history.Count, Is.EqualTo(1)); 
			Assert.That(history.ContainsKey(inRange), Is.True);
			Assert.That(history.ContainsKey(outOfRange), Is.False);
		}

		// Helper methods
		private async Task<Track> CreateTestTrackAsync(string title = "Test Track", int? genreId = null)
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
				GenreId = genreId,
				CreatedAt = DateTime.UtcNow
			};

			context.Tracks.Add(track);
			await context.SaveChangesAsync();
			return track;
		}

		private async Task<Genre> CreateTestGenreAsync(string name = "Test Genre", string color = "#667eea")
		{
			using var context = await _dbFactory.CreateDbContextAsync();
			var genre = new Genre
			{
				Name = name,
				Color = color
			};

			context.Genres.Add(genre);
			await context.SaveChangesAsync();
			return genre;
		}

		private async Task CreateHistoryRecordAsync(string userId, int trackId, DateTime date)
		{
			using var context = await _dbFactory.CreateDbContextAsync();
			var history = new ListeningHistory
			{
				UserId = userId,
				TrackId = trackId,
				PlayedAt = date,
				DurationListened = 180
			};

			context.ListeningHistory.Add(history);
			await context.SaveChangesAsync();
		}

		[TearDown]
		public void TearDown()
		{
			_dbFactory = null;
		}
	}
}