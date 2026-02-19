using NUnit.Framework;
using Piece.Services;
using System.IO;

namespace Piece.Tests.Services
{
	[TestFixture]
	public class SecurityLoggerTests
	{
		private StringWriter _consoleOutput;
		private TextWriter _originalConsoleOut;

		[SetUp]
		public void Setup()
		{
			_consoleOutput = new StringWriter();
			_originalConsoleOut = Console.Out;
			Console.SetOut(_consoleOutput);
		}

		[Test]
		public void LogUploadAttempt_WithSuccess_LogsSuccess()
		{
			// Act
			SecurityLogger.LogUploadAttempt("user123", "test.mp3", success: true);

			// Assert
			var output = _consoleOutput.ToString();
			Assert.That(output, Does.Contain("UPLOAD SUCCESS"));
			Assert.That(output, Does.Contain("user123"));
			Assert.That(output, Does.Contain("test.mp3"));
			Assert.That(output, Does.Contain("[SECURITY]"));
		}

		[Test]
		public void LogUploadAttempt_WithFailure_LogsFailure()
		{
			// Act
			SecurityLogger.LogUploadAttempt("user456", "bad.exe", success: false, reason: "Invalid format");

			// Assert
			var output = _consoleOutput.ToString();
			Assert.That(output, Does.Contain("UPLOAD FAILED"));
			Assert.That(output, Does.Contain("user456"));
			Assert.That(output, Does.Contain("bad.exe"));
			Assert.That(output, Does.Contain("Invalid format"));
		}

		[Test]
		public void LogUploadAttempt_IncludesTimestamp()
		{
			// Act
			SecurityLogger.LogUploadAttempt("user789", "file.mp3", success: true);

			// Assert
			var output = _consoleOutput.ToString();
			var currentYear = DateTime.UtcNow.Year.ToString();
			Assert.That(output, Does.Contain(currentYear));
		}

		[Test]
		public void LogSuspiciousActivity_LogsActivity()
		{
			// Act
			SecurityLogger.LogSuspiciousActivity("user111", "Multiple failed logins", "5 attempts in 1 minute");

			// Assert
			var output = _consoleOutput.ToString();
			Assert.That(output, Does.Contain("[SECURITY ALERT]"));
			Assert.That(output, Does.Contain("user111"));
			Assert.That(output, Does.Contain("Multiple failed logins"));
			Assert.That(output, Does.Contain("5 attempts in 1 minute"));
		}

		[Test]
		public void LogSuspiciousActivity_IncludesTimestamp()
		{
			// Act
			SecurityLogger.LogSuspiciousActivity("user222", "SQL Injection attempt", "Malformed query");

			// Assert
			var output = _consoleOutput.ToString();
			var currentYear = DateTime.UtcNow.Year.ToString();
			Assert.That(output, Does.Contain(currentYear));
		}

		[Test]
		public void LogRateLimitExceeded_LogsRateLimitViolation()
		{
			// Act
			SecurityLogger.LogRateLimitExceeded("user333");

			// Assert
			var output = _consoleOutput.ToString();
			Assert.That(output, Does.Contain("[SECURITY ALERT]"));
			Assert.That(output, Does.Contain("RATE LIMIT EXCEEDED"));
			Assert.That(output, Does.Contain("user333"));
		}

		[Test]
		public void LogRateLimitExceeded_IncludesTimestamp()
		{
			// Act
			SecurityLogger.LogRateLimitExceeded("user444");

			// Assert
			var output = _consoleOutput.ToString();
			var currentYear = DateTime.UtcNow.Year.ToString();
			Assert.That(output, Does.Contain(currentYear));
		}

		[Test]
		public void LogUploadAttempt_WithoutReason_DoesNotIncludeReason()
		{
			// Act
			SecurityLogger.LogUploadAttempt("user555", "file.mp3", success: true);

			// Assert
			var output = _consoleOutput.ToString();
			Assert.That(output, Does.Not.Contain("Reason:"));
		}

		[Test]
		public void LogUploadAttempt_WithEmptyReason_DoesNotIncludeReason()
		{
			// Act
			SecurityLogger.LogUploadAttempt("user666", "file.mp3", success: false, reason: "");

			// Assert
			var output = _consoleOutput.ToString();
			Assert.That(output, Does.Not.Contain("Reason:"));
		}

		[TearDown]
		public void TearDown()
		{
			Console.SetOut(_originalConsoleOut);
			_consoleOutput?.Dispose();
		}
	}
}