using NUnit.Framework;
using Piece.Services;
using System.IO;

namespace Piece.Tests.Services
{
	[TestFixture]
	public class AudioMetadataReaderTests
	{
		private string _testFilesDirectory;

		[SetUp]
		public void Setup()
		{
			var testDirectory = TestContext.CurrentContext.TestDirectory;
			_testFilesDirectory = Path.Combine(testDirectory, "TestFiles");

			Console.WriteLine($"[Test Setup] Looking for test files in: {_testFilesDirectory}");
			Console.WriteLine($"[Test Setup] Directory exists: {Directory.Exists(_testFilesDirectory)}");

			if (Directory.Exists(_testFilesDirectory))
			{
				var files = Directory.GetFiles(_testFilesDirectory, "*.mp3");
				Console.WriteLine($"[Test Setup] Found {files.Length} MP3 files");
				foreach (var file in files)
				{
					Console.WriteLine($"  - {Path.GetFileName(file)}");
				}
			}
		}

		[Test]
		public void GetDurationSeconds_WithValidMp3_ReturnsDuration()
		{
			// Arrange 
			var mp3Path = Path.Combine(_testFilesDirectory, "freesound1.mp3");

			// Verify file exists
			Assert.That(File.Exists(mp3Path), Is.True,
				$"MP3 file not found at: {mp3Path}");

			// Act
			var duration = AudioMetadataReader.GetDurationSeconds(mp3Path);

			// Assert
			Assert.That(duration, Is.GreaterThan(0),
				"Duration should be greater than 0 for valid MP3");
			Assert.That(duration, Is.LessThan(3600),
				"Duration should be less than 1 hour (sanity check)");

			Console.WriteLine($"[Test] MP3 duration: {duration} seconds");
		}

		[Test]
		public void GetDurationSeconds_WithMultipleValidMp3s_AllReturnDuration()
		{
			// Arrange - Test all MP3 files
			var mp3Files = new[]
			{
				"freesound1.mp3",
				"freesound2.mp3",
				"freesound3.mp3"
			};

			foreach (var fileName in mp3Files)
			{
				var mp3Path = Path.Combine(_testFilesDirectory, fileName);

				if (!File.Exists(mp3Path))
				{
					Console.WriteLine($"[Test] Skipping {fileName} - file not found");
					continue;
				}

				// Act
				var duration = AudioMetadataReader.GetDurationSeconds(mp3Path);

				// Assert
				Assert.That(duration, Is.GreaterThan(0),
					$"{fileName} should have duration > 0");

				Console.WriteLine($"[Test] {fileName}: {duration} seconds");
			}
		}

		[Test]
		public void GetDurationSeconds_WithInvalidFile_ReturnsZero()
		{
			// Arrange
			var invalidFilePath = Path.Combine(_testFilesDirectory, "notmp3.txt");
			File.WriteAllText(invalidFilePath, "This is not an MP3 file");

			// Act
			var duration = AudioMetadataReader.GetDurationSeconds(invalidFilePath);

			// Assert
			Assert.That(duration, Is.EqualTo(0));

			// Cleanup
			if (File.Exists(invalidFilePath))
				File.Delete(invalidFilePath);
		}

		[Test]
		public void GetDurationSeconds_WithNonExistentFile_ReturnsZero()
		{
			// Arrange
			var nonExistentPath = Path.Combine(_testFilesDirectory, "doesnotexist.mp3");

			// Act
			var duration = AudioMetadataReader.GetDurationSeconds(nonExistentPath);

			// Assert
			Assert.That(duration, Is.EqualTo(0));
		}

		[Test]
		public void GetDurationSeconds_WithEmptyFile_ReturnsZero()
		{
			// Arrange
			var emptyFilePath = Path.Combine(_testFilesDirectory, "empty.mp3");
			File.WriteAllBytes(emptyFilePath, new byte[0]);

			// Act
			var duration = AudioMetadataReader.GetDurationSeconds(emptyFilePath);

			// Assert
			Assert.That(duration, Is.EqualTo(0));

			// Cleanup
			if (File.Exists(emptyFilePath))
				File.Delete(emptyFilePath);
		}

		[Test]
		public void GetDurationSeconds_HandlesExceptionsGracefully()
		{
			// Arrange
			var corruptedFilePath = Path.Combine(_testFilesDirectory, "corrupted.mp3");
			var fakeHeader = new byte[] { 0xFF, 0xFB, 0x90, 0x44, 0x00, 0x00 };
			File.WriteAllBytes(corruptedFilePath, fakeHeader);

			// Act
			var duration = AudioMetadataReader.GetDurationSeconds(corruptedFilePath);

			// Assert
			Assert.That(duration, Is.EqualTo(0));

			// Cleanup
			if (File.Exists(corruptedFilePath))
				File.Delete(corruptedFilePath);
		}

		[TearDown]
		public void TearDown()
		{
		}
	}
}