using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Piece.Data;
using Piece.Services;
using Piece.Data.Models;
using Piece.Data.Enums;

namespace Piece.Tests.Services
{
	[TestFixture]
	public class PlaylistServiceTests
	{
		private IDbContextFactory<ApplicationDbContext> _dbFactory;
		private PlaylistService _service;

		[SetUp]
		public void Setup()
		{
			var options = new DbContextOptionsBuilder<ApplicationDbContext>()
				.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
				.Options;

			var factory = new TestDbContextFactory(options);
			_dbFactory = factory;
			_service = new PlaylistService(_dbFactory);
		}

		[Test]
		public async Task CreatePlaylistAsync_CreatesNewPlaylist()
		{
			// Arrange
			var userId = "test-user";
			var name = "My Playlist";
			var description = "Test description";

			// Act
			var playlist = await _service.CreatePlaylistAsync(userId, name, description, isPublic: true);

			// Assert
			Assert.That(playlist, Is.Not.Null);
			Assert.That(playlist.Name, Is.EqualTo(name));
			Assert.That(playlist.UserId, Is.EqualTo(userId));
			Assert.That(playlist.Description, Is.EqualTo(description));
			Assert.That(playlist.IsPublic, Is.True);
		}

		[Test]
		public async Task GetUserPlaylistsAsync_ReturnsOnlyUserPlaylists()
		{
			// Arrange
			var userId1 = "user1";
			var userId2 = "user2";

			await _service.CreatePlaylistAsync(userId1, "Playlist 1");
			await _service.CreatePlaylistAsync(userId1, "Playlist 2");
			await _service.CreatePlaylistAsync(userId2, "Other Playlist");

			// Act
			var playlists = await _service.GetUserPlaylistsAsync(userId1);

			// Assert
			Assert.That(playlists.Count, Is.EqualTo(2));
			Assert.That(playlists.All(p => p.UserId == userId1), Is.True);
		}

		[Test]
		public async Task GetPlaylistByIdAsync_ReturnsCorrectPlaylist()
		{
			// Arrange
			var userId = "user1";
			var created = await _service.CreatePlaylistAsync(userId, "Test Playlist");

			// Act
			var playlist = await _service.GetPlaylistByIdAsync(created.Id, userId);

			// Assert
			Assert.That(playlist, Is.Not.Null);
			Assert.That(playlist.Id, Is.EqualTo(created.Id));
			Assert.That(playlist.Name, Is.EqualTo("Test Playlist"));
		}

		[Test]
		public async Task UpdatePlaylistAsync_UpdatesProperties()
		{
			// Arrange
			var userId = "user1";
			var playlist = await _service.CreatePlaylistAsync(userId, "Original Name");

			// Act
			var result = await _service.UpdatePlaylistAsync(
				playlist.Id,
				userId,
				"Updated Name",
				"New description",
				isPublic: false
			);

			// Assert
			Assert.That(result, Is.True);
			var updated = await _service.GetPlaylistByIdAsync(playlist.Id, userId);
			Assert.That(updated.Name, Is.EqualTo("Updated Name"));
			Assert.That(updated.Description, Is.EqualTo("New description"));
			Assert.That(updated.IsPublic, Is.False);
		}

		[Test]
		public async Task DeletePlaylistAsync_RemovesPlaylist()
		{
			// Arrange
			var userId = "user1";
			var playlist = await _service.CreatePlaylistAsync(userId, "To Delete");

			// Act
			var result = await _service.DeletePlaylistAsync(playlist.Id, userId);

			// Assert
			Assert.That(result, Is.True);
			var deleted = await _service.GetPlaylistByIdAsync(playlist.Id, userId);
			Assert.That(deleted, Is.Null);
		}

		[Test]
		public async Task AddTrackToPlaylistAsync_AddsTrack()
		{
			// Arrange
			var userId = "user1";
			var playlist = await _service.CreatePlaylistAsync(userId, "Test");
			var track = await CreateTestTrackAsync();

			// Act
			var result = await _service.AddTrackToPlaylistAsync(playlist.Id, track.Id, userId);

			// Assert
			Assert.That(result, Is.True);
			var tracks = await _service.GetPlaylistTracksAsync(playlist.Id, userId);
			Assert.That(tracks.Count, Is.EqualTo(1));
			Assert.That(tracks[0].Id, Is.EqualTo(track.Id));
		}

		[Test]
		public async Task AddTrackToPlaylistAsync_PreventsDuplicates()
		{
			// Arrange
			var userId = "user1";
			var playlist = await _service.CreatePlaylistAsync(userId, "Test");
			var track = await CreateTestTrackAsync();
			await _service.AddTrackToPlaylistAsync(playlist.Id, track.Id, userId);

			// Act
			var result = await _service.AddTrackToPlaylistAsync(playlist.Id, track.Id, userId);

			// Assert
			Assert.That(result, Is.False); // Should return false for duplicate
		}

		[Test]
		public async Task RemoveTrackFromPlaylistAsync_RemovesTrack()
		{
			// Arrange
			var userId = "user1";
			var playlist = await _service.CreatePlaylistAsync(userId, "Test");
			var track = await CreateTestTrackAsync();
			await _service.AddTrackToPlaylistAsync(playlist.Id, track.Id, userId);

			// Act
			var result = await _service.RemoveTrackFromPlaylistAsync(playlist.Id, track.Id, userId);

			// Assert
			Assert.That(result, Is.True);
			var tracks = await _service.GetPlaylistTracksAsync(playlist.Id, userId);
			Assert.That(tracks.Count, Is.EqualTo(0));
		}

		[Test]
		public async Task GetPlaylistTracksAsync_ReturnsTracksInOrder()
		{
			// Arrange
			var userId = "user1";
			var playlist = await _service.CreatePlaylistAsync(userId, "Test");
			var track1 = await CreateTestTrackAsync("Track 1");
			var track2 = await CreateTestTrackAsync("Track 2");
			var track3 = await CreateTestTrackAsync("Track 3");

			await _service.AddTrackToPlaylistAsync(playlist.Id, track1.Id, userId);
			await _service.AddTrackToPlaylistAsync(playlist.Id, track2.Id, userId);
			await _service.AddTrackToPlaylistAsync(playlist.Id, track3.Id, userId);

			// Act
			var tracks = await _service.GetPlaylistTracksAsync(playlist.Id, userId);

			// Assert
			Assert.That(tracks.Count, Is.EqualTo(3));
			Assert.That(tracks[0].Title, Is.EqualTo("Track 1"));
			Assert.That(tracks[1].Title, Is.EqualTo("Track 2"));
			Assert.That(tracks[2].Title, Is.EqualTo("Track 3"));
		}

		// Helper methods
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

	public class TestDbContextFactory : IDbContextFactory<ApplicationDbContext>
	{
		private readonly DbContextOptions<ApplicationDbContext> _options;

		public TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
		{
			_options = options;
		}

		public ApplicationDbContext CreateDbContext()
		{
			return new ApplicationDbContext(_options);
		}

		public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
		{
			return Task.FromResult(new ApplicationDbContext(_options));
		}
	}
}