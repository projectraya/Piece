using NUnit.Framework;
using Piece.Services;

namespace Piece.Tests.Services
{
	[TestFixture]
	public class UploadRateLimiterTests
	{
		[SetUp]
		public void Setup()
		{
			// In production, consider making it instance-based or adding a Reset method for testing
		}

		[Test]
		public void CanUpload_WithNoHistory_ReturnsTrue()
		{
			// Arrange
			var userId = Guid.NewGuid().ToString();

			// Act
			var result = UploadRateLimiter.CanUpload(userId);

			// Assert
			Assert.That(result, Is.True);
		}

		[Test]
		public void CanUpload_AfterOneUpload_ReturnsTrue()
		{
			// Arrange
			var userId = Guid.NewGuid().ToString();
			UploadRateLimiter.RecordUploadAttempt(userId);

			// Act
			var result = UploadRateLimiter.CanUpload(userId);

			// Assert
			Assert.That(result, Is.True);
		}

		[Test]
		public void CanUpload_After50Uploads_ReturnsFalse()
		{
			// Arrange
			var userId = Guid.NewGuid().ToString();

			for (int i = 0; i < 50; i++)
			{
				UploadRateLimiter.RecordUploadAttempt(userId);
			}

			// Act
			var result = UploadRateLimiter.CanUpload(userId);

			// Assert
			Assert.That(result, Is.False);
		}

		[Test]
		public void RecordUploadAttempt_IncreasesAttemptCount()
		{
			// Arrange
			var userId = Guid.NewGuid().ToString();

			// Act
			UploadRateLimiter.RecordUploadAttempt(userId);
			UploadRateLimiter.RecordUploadAttempt(userId);
			UploadRateLimiter.RecordUploadAttempt(userId);

			// Assert 
			Assert.That(UploadRateLimiter.CanUpload(userId), Is.True);
		}

		[Test]
		public void HasTooManyFailedAttempts_WithNoAttempts_ReturnsFalse()
		{
			// Arrange
			var userId = Guid.NewGuid().ToString();

			// Act
			var result = UploadRateLimiter.HasTooManyFailedAttempts(userId);

			// Assert
			Assert.That(result, Is.False);
		}

		[Test]
		public void HasTooManyFailedAttempts_WithFewAttempts_ReturnsFalse()
		{
			// Arrange
			var userId = Guid.NewGuid().ToString();

			for (int i = 0; i < 5; i++)
			{
				UploadRateLimiter.RecordUploadAttempt(userId);
			}

			// Act
			var result = UploadRateLimiter.HasTooManyFailedAttempts(userId);

			// Assert
			Assert.That(result, Is.False);
		}

		[Test]
		public void HasTooManyFailedAttempts_With10Attempts_ReturnsTrue()
		{
			// Arrange
			var userId = Guid.NewGuid().ToString();

			for (int i = 0; i < 10; i++)
			{
				UploadRateLimiter.RecordUploadAttempt(userId);
			}

			// Act
			var result = UploadRateLimiter.HasTooManyFailedAttempts(userId);

			// Assert
			Assert.That(result, Is.True);
		}

		[Test]
		public void CanUpload_DifferentUsers_AreIndependent()
		{
			// Arrange
			var user1 = Guid.NewGuid().ToString();
			var user2 = Guid.NewGuid().ToString();

			for (int i = 0; i < 50; i++)
			{
				UploadRateLimiter.RecordUploadAttempt(user1);
			}

			// Act
			var user1CanUpload = UploadRateLimiter.CanUpload(user1);
			var user2CanUpload = UploadRateLimiter.CanUpload(user2);

			// Assert
			Assert.That(user1CanUpload, Is.False);
			Assert.That(user2CanUpload, Is.True);
		}

		[Test]
		public void RecordUploadAttempt_IsThreadSafe()
		{
			// Arrange
			var userId = Guid.NewGuid().ToString();
			var tasks = new List<Task>();

			// Act 
			for (int i = 0; i < 10; i++)
			{
				tasks.Add(Task.Run(() => UploadRateLimiter.RecordUploadAttempt(userId)));
			}

			Task.WaitAll(tasks.ToArray());

			// Assert 
			Assert.That(UploadRateLimiter.CanUpload(userId), Is.True);
		}

		[Test]
		public void CanUpload_MultipleQuickChecks_RemainConsistent()
		{
			// Arrange
			var userId = Guid.NewGuid().ToString();

			// Act 
			var result1 = UploadRateLimiter.CanUpload(userId);
			var result2 = UploadRateLimiter.CanUpload(userId);
			var result3 = UploadRateLimiter.CanUpload(userId);

			// Assert
			Assert.That(result1, Is.True);
			Assert.That(result2, Is.True);
			Assert.That(result3, Is.True);
		}
	}
}