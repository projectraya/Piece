using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Piece.Data;
using Piece.Services;
using Piece.Data.Models;

namespace Piece.Tests.Services
{
	[TestFixture]
	public class ProfileServiceTests
	{
		private IDbContextFactory<ApplicationDbContext> _dbFactory;
		private ProfileService _service;

		[SetUp]
		public void Setup()
		{
			var options = new DbContextOptionsBuilder<ApplicationDbContext>()
				.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
				.Options;

			var factory = new TestDbContextFactory(options);
			_dbFactory = factory;
			_service = new ProfileService(_dbFactory);
		}

		[Test]
		public async Task GetUserProfileAsync_ReturnsUser()
		{
			// Arrange
			var user = await CreateTestUserAsync();

			// Act
			var result = await _service.GetUserProfileAsync(user.Id);

			// Assert
			Assert.That(result, Is.Not.Null);
			Assert.That(result.Id, Is.EqualTo(user.Id));
		}

		[Test]
		public async Task GetUserProfileByUsernameAsync_ReturnsUser()
		{
			// Arrange
			var user = await CreateTestUserAsync(username: "exactusername");

			// Act 
			var result = await _service.GetUserProfileByUsernameAsync(user.UserName);

			// Assert
			Assert.That(result, Is.Not.Null);
			Assert.That(result.UserName, Is.EqualTo(user.UserName));
		}

		[Test]
		public async Task GetUserProfileByEmailAsync_ReturnsUser()
		{
			// Arrange
			var user = await CreateTestUserAsync(email: "exact@example.com");

			// Act 
			var result = await _service.GetUserProfileByEmailAsync(user.Email);

			// Assert
			Assert.That(result, Is.Not.Null);
			Assert.That(result.Email, Is.EqualTo(user.Email));
		}

		[Test]
		public async Task UpdateProfileAsync_UpdatesUserProperties()
		{
			// Arrange
			var user = await CreateTestUserAsync();

			// Act
			var result = await _service.UpdateProfileAsync(
				user.Id,
				"New Display Name",
				"New Bio",
				isPublic: true,
				showHistory: true,
				showPlaylists: true
			);

			// Assert
			Assert.That(result, Is.True);
			var updated = await _service.GetUserProfileAsync(user.Id);
			Assert.That(updated.DisplayName, Is.EqualTo("New Display Name"));
			Assert.That(updated.Bio, Is.EqualTo("New Bio"));
			Assert.That(updated.IsProfilePublic, Is.True);
			Assert.That(updated.ShowListeningHistory, Is.True);
			Assert.That(updated.ShowPlaylists, Is.True);
		}

		[Test]
		public async Task UpdateProfileAsync_TrimsWhitespace()
		{
			// Arrange
			var user = await CreateTestUserAsync();

			// Act
			await _service.UpdateProfileAsync(
				user.Id,
				"  Trimmed Name  ",
				"  Trimmed Bio  ",
				isPublic: true,
				showHistory: true,
				showPlaylists: true
			);

			// Assert
			var updated = await _service.GetUserProfileAsync(user.Id);
			Assert.That(updated.DisplayName, Is.EqualTo("Trimmed Name"));
			Assert.That(updated.Bio, Is.EqualTo("Trimmed Bio"));
		}

		[Test]
		public async Task UpdateProfileAsync_WithProfilePicture_UpdatesPicture()
		{
			// Arrange
			var user = await CreateTestUserAsync();
			var pictureUrl = "https://example.com/pic.jpg";

			// Act
			var result = await _service.UpdateProfileAsync(
				user.Id,
				"Name",
				"Bio",
				isProfilePublic: true,
				showListeningHistory: true,
				showPlaylists: true,
				profilePictureUrl: pictureUrl
			);

			// Assert
			Assert.That(result, Is.True);
			var updated = await _service.GetUserProfileAsync(user.Id);
			Assert.That(updated.ProfilePictureUrl, Is.EqualTo(pictureUrl));
		}

		[Test]
		public async Task UpdateProfileAsync_WithInvalidUserId_ReturnsFalse()
		{
			// Act
			var result = await _service.UpdateProfileAsync(
				"invalid-user-id",
				"Name",
				"Bio",
				isPublic: true,
				showHistory: true,
				showPlaylists: true
			);

			// Assert
			Assert.That(result, Is.False);
		}

		[Test]
		public async Task SearchUsersAsync_ReturnsPublicUsers()
		{
			// Arrange
			await CreateTestUserAsync("user1", "User One", isPublic: true);
			await CreateTestUserAsync("user2", "User Two", isPublic: true);
			await CreateTestUserAsync("user3", "Secret User", isPublic: false);

			// Act
			var results = await _service.SearchUsersAsync("User");

			// Assert
			Assert.That(results.Count, Is.EqualTo(2));
			Assert.That(results.All(u => u.IsProfilePublic), Is.True);
		}

		[Test]
		public async Task SearchUsersAsync_CaseInsensitive()
		{
			// Arrange
			await CreateTestUserAsync(displayName: "TestUser", isPublic: true);

			// Act
			var results = await _service.SearchUsersAsync("testuser");

			// Assert
			Assert.That(results.Count, Is.EqualTo(1));
		}

		[Test]
		public async Task SearchUsersAsync_WithEmptyQuery_ReturnsEmpty()
		{
			// Arrange
			await CreateTestUserAsync(isPublic: true);

			// Act
			var results = await _service.SearchUsersAsync("");

			// Assert
			Assert.That(results.Count, Is.EqualTo(0));
		}

		[Test]
		public async Task SearchUsersAsync_RespectsLimit()
		{
			// Arrange
			for (int i = 0; i < 30; i++)
			{
				await CreateTestUserAsync($"user{i}", $"User {i}", isPublic: true);
			}

			// Act
			var results = await _service.SearchUsersAsync("User", limit: 10);

			// Assert
			Assert.That(results.Count, Is.EqualTo(10));
		}

		[Test]
		public async Task GetUserPublicPlaylistsAsync_ReturnsPublicPlaylists()
		{
			// Arrange
			var user = await CreateTestUserAsync();
			user.ShowPlaylists = true;
			await UpdateUserAsync(user);

			await CreateTestPlaylistAsync(user.Id, "Public 1", isPublic: true);
			await CreateTestPlaylistAsync(user.Id, "Public 2", isPublic: true);
			await CreateTestPlaylistAsync(user.Id, "Private", isPublic: false);

			// Act
			var playlists = await _service.GetUserPublicPlaylistsAsync(user.Id);

			// Assert
			Assert.That(playlists.Count, Is.EqualTo(2));
			Assert.That(playlists.All(p => p.IsPublic), Is.True);
		}

		[Test]
		public async Task GetUserPublicPlaylistsAsync_WhenShowPlaylistsFalse_ReturnsEmpty()
		{
			// Arrange
			var user = await CreateTestUserAsync();
			user.ShowPlaylists = false;
			await UpdateUserAsync(user);

			await CreateTestPlaylistAsync(user.Id, "Public", isPublic: true);

			// Act
			var playlists = await _service.GetUserPublicPlaylistsAsync(user.Id);

			// Assert
			Assert.That(playlists.Count, Is.EqualTo(0));
		}

		[Test]
		public async Task UpdateLastActiveAsync_UpdatesTimestamp()
		{
			// Arrange
			var user = await CreateTestUserAsync();
			var oldTimestamp = user.LastActiveAt;

			await Task.Delay(100); // Small delay to ensure timestamp difference

			// Act
			await _service.UpdateLastActiveAsync(user.Id);

			// Assert
			var updated = await _service.GetUserProfileAsync(user.Id);
			Assert.That(updated.LastActiveAt, Is.GreaterThan(oldTimestamp ?? DateTime.MinValue));
		}

		// Helper methods
		private async Task<ApplicationUser> CreateTestUserAsync(
			string username = "testuser",
			string displayName = "Test User",
			string email = "test@example.com",
			bool isPublic = true)
		{
			using var context = await _dbFactory.CreateDbContextAsync();
			var user = new ApplicationUser
			{
				Id = Guid.NewGuid().ToString(),
				UserName = username + Guid.NewGuid().ToString().Substring(0, 8),
				Email = email + Guid.NewGuid().ToString().Substring(0, 8),
				DisplayName = displayName,
				IsProfilePublic = isPublic,
				ShowPlaylists = true,
				ShowListeningHistory = true,
				CreatedAt = DateTime.UtcNow,
				LastActiveAt = DateTime.UtcNow
			};

			context.Users.Add(user);
			await context.SaveChangesAsync();
			return user;
		}

		private async Task UpdateUserAsync(ApplicationUser user)
		{
			using var context = await _dbFactory.CreateDbContextAsync();
			context.Users.Update(user);
			await context.SaveChangesAsync();
		}

		private async Task<Playlist> CreateTestPlaylistAsync(string userId, string name, bool isPublic)
		{
			using var context = await _dbFactory.CreateDbContextAsync();
			var playlist = new Playlist
			{
				UserId = userId,
				Name = name,
				IsPublic = isPublic,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};

			context.Playlists.Add(playlist);
			await context.SaveChangesAsync();
			return playlist;
		}

		[TearDown]
		public void TearDown()
		{
			_dbFactory = null;
		}
	}
}