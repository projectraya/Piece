using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Piece.Data;
using Piece.Services;
using Piece.Data.Models;

namespace Piece.Tests.Services
{
	[TestFixture]
	public class ActivityLoggerTests
	{
		private IDbContextFactory<ApplicationDbContext> _dbFactory;
		private ActivityLogger _service;

		[SetUp]
		public void Setup()
		{
			var options = new DbContextOptionsBuilder<ApplicationDbContext>()
				.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
				.Options;

			var factory = new TestDbContextFactory(options);
			_dbFactory = factory;
			_service = new ActivityLogger(_dbFactory);
		}

		[Test]
		public async Task LogAsync_CreatesLogEntry()
		{
			// Arrange
			var eventType = "USER_LOGIN";
			var message = "User logged in";
			var performedBy = "user1";

			// Act
			await _service.LogAsync(eventType, message, performedBy);

			// Assert
			using var context = await _dbFactory.CreateDbContextAsync();
			var log = await context.ActivityLogs.FirstOrDefaultAsync();

			Assert.That(log, Is.Not.Null);
			Assert.That(log.EventType, Is.EqualTo(eventType));
			Assert.That(log.Message, Is.EqualTo(message));
			Assert.That(log.PerformedBy, Is.EqualTo(performedBy));
			Assert.That(log.Severity, Is.EqualTo("Info")); 
		}

		[Test]
		public async Task LogAsync_WithAllParameters_StoresAllData()
		{
			// Act
			await _service.LogAsync(
				eventType: "TRACK_DELETE",
				message: "Track deleted",
				performedBy: "admin",
				targetEntity: "Track:123",
				additionalInfo: "Reason: Duplicate",
				severity: "Warning"
			);

			// Assert
			using var context = await _dbFactory.CreateDbContextAsync();
			var log = await context.ActivityLogs.FirstOrDefaultAsync();

			Assert.That(log.TargetEntity, Is.EqualTo("Track:123"));
			Assert.That(log.AdditionalInfo, Is.EqualTo("Reason: Duplicate"));
			Assert.That(log.Severity, Is.EqualTo("Warning"));
		}

		[Test]
		public async Task GetRecentLogsAsync_ReturnsLogsOrderedByTimestamp()
		{
			// Arrange
			await _service.LogAsync("EVENT1", "First", "user1");
			await Task.Delay(10);
			await _service.LogAsync("EVENT2", "Second", "user1");
			await Task.Delay(10);
			await _service.LogAsync("EVENT3", "Third", "user1");

			// Act
			var logs = await _service.GetRecentLogsAsync(10);

			// Assert
			Assert.That(logs.Count, Is.EqualTo(3));
			Assert.That(logs[0].Message, Is.EqualTo("Third")); 
			Assert.That(logs[1].Message, Is.EqualTo("Second"));
			Assert.That(logs[2].Message, Is.EqualTo("First"));
		}

		[Test]
		public async Task GetRecentLogsAsync_RespectsLimit()
		{
			// Arrange
			for (int i = 0; i < 150; i++)
			{
				await _service.LogAsync("EVENT", $"Message {i}", "user1");
			}

			// Act
			var logs = await _service.GetRecentLogsAsync(100);

			// Assert
			Assert.That(logs.Count, Is.EqualTo(100));
		}

		[Test]
		public async Task GetFilteredLogsAsync_FiltersByEventType()
		{
			// Arrange
			await _service.LogAsync("LOGIN", "Login event", "user1");
			await _service.LogAsync("LOGOUT", "Logout event", "user1");
			await _service.LogAsync("LOGIN", "Another login", "user2");

			// Act
			var logs = await _service.GetFilteredLogsAsync(eventType: "LOGIN");

			// Assert
			Assert.That(logs.Count, Is.EqualTo(2));
			Assert.That(logs.All(l => l.EventType == "LOGIN"), Is.True);
		}

		[Test]
		public async Task GetFilteredLogsAsync_FiltersByDateRange()
		{
			// Arrange
			var yesterday = DateTime.UtcNow.AddDays(-1);
			var today = DateTime.UtcNow;
			var tomorrow = DateTime.UtcNow.AddDays(1);

			await CreateLogWithDateAsync("EVENT1", yesterday);
			await CreateLogWithDateAsync("EVENT2", today);
			await CreateLogWithDateAsync("EVENT3", tomorrow);

			// Act
			var logs = await _service.GetFilteredLogsAsync(
				startDate: yesterday.AddHours(-1),
				endDate: today.AddHours(1)
			);

			// Assert
			Assert.That(logs.Count, Is.EqualTo(2));
		}

		[Test]
		public async Task GetFilteredLogsAsync_CombinesFilters()
		{
			// Arrange
			var today = DateTime.UtcNow;
			await CreateLogWithDateAsync("LOGIN", today, "user1");
			await CreateLogWithDateAsync("LOGOUT", today, "user1");
			await CreateLogWithDateAsync("LOGIN", today.AddDays(-2), "user1");

			// Act
			var logs = await _service.GetFilteredLogsAsync(
				eventType: "LOGIN",
				startDate: today.AddDays(-1)
			);

			// Assert
			Assert.That(logs.Count, Is.EqualTo(1));
			Assert.That(logs[0].EventType, Is.EqualTo("LOGIN"));
		}

		[Test]
		public async Task GetFilteredLogsAsync_WithNoFilters_ReturnsAll()
		{
			// Arrange
			await _service.LogAsync("EVENT1", "Message 1", "user1");
			await _service.LogAsync("EVENT2", "Message 2", "user2");

			// Act
			var logs = await _service.GetFilteredLogsAsync();

			// Assert
			Assert.That(logs.Count, Is.EqualTo(2));
		}

		// Helper method
		private async Task CreateLogWithDateAsync(string eventType, DateTime timestamp, string user = "testuser")
		{
			using var context = await _dbFactory.CreateDbContextAsync();
			var log = new ActivityLog
			{
				EventType = eventType,
				Message = $"{eventType} message",
				PerformedBy = user,
				Timestamp = timestamp,
				Severity = "Info"
			};

			context.ActivityLogs.Add(log);
			await context.SaveChangesAsync();
		}

		[TearDown]
		public void TearDown()
		{
			_dbFactory = null;
		}
	}
}