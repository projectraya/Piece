using NUnit.Framework;
using System.IO;
using Piece.Services;

namespace Piece.Tests.Services
{
	[TestFixture]
	public class FileValidatorTests
	{
		private string _testFilesDirectory;

		[SetUp]
		public void Setup()
		{
			_testFilesDirectory = Path.Combine(Path.GetTempPath(), "FileValidatorTests");
			Directory.CreateDirectory(_testFilesDirectory);
		}

		[TestCase(".mp3", true)]
		[TestCase(".wav", true)]
		[TestCase(".flac", true)]
		[TestCase(".m4a", true)]
		[TestCase(".exe", false)]
		[TestCase(".txt", false)]
		[TestCase(".MP3", true)]
		public void IsValidAudioExtension_ValidatesCorrectly(string extension, bool expected)
		{
			// Arrange
			var fileName = "test" + extension;

			// Act
			var result = FileValidator.IsValidAudioExtension(fileName);

			// Assert
			Assert.That(result, Is.EqualTo(expected));
		}

		[TestCase(".jpg", true)]
		[TestCase(".jpeg", true)]
		[TestCase(".png", true)]
		[TestCase(".webp", true)]
		[TestCase(".gif", false)]
		[TestCase(".bmp", false)]
		[TestCase(".PNG", true)] 
		public void IsValidImageExtension_ValidatesCorrectly(string extension, bool expected)
		{
			// Arrange
			var fileName = "test" + extension;

			// Act
			var result = FileValidator.IsValidImageExtension(fileName);

			// Assert
			Assert.That(result, Is.EqualTo(expected));
		}

		[Test]
		public void ValidateAudioFileMagicBytes_MP3_WithID3Tag_ReturnsTrue()
		{
			// Arrange
			var filePath = Path.Combine(_testFilesDirectory, "test_id3.mp3");
			var mp3Header = new byte[] { 0x49, 0x44, 0x33, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
			File.WriteAllBytes(filePath, mp3Header);

			// Act
			var result = FileValidator.ValidateAudioFileMagicBytes(filePath);

			// Assert
			Assert.That(result, Is.True);
		}

		[Test]
		public void ValidateAudioFileMagicBytes_MP3_WithMPEGSync_ReturnsTrue()
		{
			// Arrange
			var filePath = Path.Combine(_testFilesDirectory, "test_mpeg.mp3");
			var mpegHeader = new byte[] { 0xFF, 0xFB, 0x90, 0x44, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
			File.WriteAllBytes(filePath, mpegHeader);

			// Act
			var result = FileValidator.ValidateAudioFileMagicBytes(filePath);

			// Assert
			Assert.That(result, Is.True);
		}

		[Test]
		public void ValidateAudioFileMagicBytes_InvalidFile_ReturnsFalse()
		{
			// Arrange
			var filePath = Path.Combine(_testFilesDirectory, "test_invalid.mp3");
			var invalidHeader = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00 };
			File.WriteAllBytes(filePath, invalidHeader);

			// Act
			var result = FileValidator.ValidateAudioFileMagicBytes(filePath);

			// Assert
			Assert.That(result, Is.False);
		}

		[Test]
		public void ValidateImageFileMagicBytes_PNG_ReturnsTrue()
		{
			// Arrange
			var filePath = Path.Combine(_testFilesDirectory, "test.png");
			var pngHeader = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
			File.WriteAllBytes(filePath, pngHeader);

			// Act
			var result = FileValidator.ValidateImageFileMagicBytes(filePath);

			// Assert
			Assert.That(result, Is.True);
		}

		[Test]
		public void ValidateImageFileMagicBytes_JPEG_ReturnsTrue()
		{
			// Arrange
			var filePath = Path.Combine(_testFilesDirectory, "test.jpg");
			var jpegHeader = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 };
			File.WriteAllBytes(filePath, jpegHeader);

			// Act
			var result = FileValidator.ValidateImageFileMagicBytes(filePath);

			// Assert
			Assert.That(result, Is.True);
		}

		[Test]
		public void SanitizeFileName_RemovesInvalidCharacters()
		{
			// Arrange
			var fileName = "test<>:\"/\\|?*.mp3";

			// Act
			var result = FileValidator.SanitizeFileName(fileName);

			// Assert
			Assert.That(result.Contains("<"), Is.False);
			Assert.That(result.Contains(">"), Is.False);
			Assert.That(result.Contains(":"), Is.False);
			Assert.That(result.Contains("*"), Is.False);
		}

		[Test]
		public void SanitizeFileName_RemovesDangerousCharacters()
		{
			// Arrange
			var fileName = "test$`{}[].mp3";

			// Act
			var result = FileValidator.SanitizeFileName(fileName);

			// Assert
			Assert.That(result.Contains("$"), Is.False);
			Assert.That(result.Contains("`"), Is.False);
			Assert.That(result.Contains("{"), Is.False);
			Assert.That(result.Contains("["), Is.False);
		}

		[Test]
		public void SanitizeFileName_TruncatesLongNames()
		{
			// Arrange
			var longName = new string('a', 300) + ".mp3";

			// Act
			var result = FileValidator.SanitizeFileName(longName);

			// Assert
			Assert.That(result.Length, Is.LessThanOrEqualTo(255));
			Assert.That(result, Does.EndWith(".mp3"));
		}

		[TestCase(null, "unnamed")]
		[TestCase("", "unnamed")]
		[TestCase("   ", "unnamed")]
		public void SanitizeFileName_HandlesNullOrEmpty(string input, string expected)
		{
			// Act
			var result = FileValidator.SanitizeFileName(input);

			// Assert
			Assert.That(result, Is.EqualTo(expected));
		}

		[Test]
		public void SanitizeFileName_PreservesValidFileName()
		{
			// Arrange
			var fileName = "valid_file-name.mp3";

			// Act
			var result = FileValidator.SanitizeFileName(fileName);

			// Assert
			Assert.That(result, Is.EqualTo(fileName));
		}

		[Test]
		public void CalculateFileHash_ReturnsConsistentHash()
		{
			// Arrange
			var filePath = Path.Combine(_testFilesDirectory, "hashtest.txt");
			File.WriteAllText(filePath, "Test content for hashing");

			// Act
			var hash1 = FileValidator.CalculateFileHash(filePath);
			var hash2 = FileValidator.CalculateFileHash(filePath);

			// Assert
			Assert.That(hash1, Is.EqualTo(hash2));
			Assert.That(hash1, Is.Not.Empty);
		}

		[Test]
		public void CalculateFileHash_DifferentFiles_ProduceDifferentHashes()
		{
			// Arrange
			var file1 = Path.Combine(_testFilesDirectory, "file1.txt");
			var file2 = Path.Combine(_testFilesDirectory, "file2.txt");
			File.WriteAllText(file1, "Content 1");
			File.WriteAllText(file2, "Content 2");

			// Act
			var hash1 = FileValidator.CalculateFileHash(file1);
			var hash2 = FileValidator.CalculateFileHash(file2);

			// Assert
			Assert.That(hash1, Is.Not.EqualTo(hash2));
		}

		[Test]
		public void HasSufficientDiskSpace_WithEnoughSpace_ReturnsTrue()
		{
			// Arrange
			var path = _testFilesDirectory;

			// Act
			var result = FileValidator.HasSufficientDiskSpace(path);

			// Assert 
			Assert.That(result, Is.True);
		}

		[TearDown]
		public void TearDown()
		{
			if (Directory.Exists(_testFilesDirectory))
			{
				Directory.Delete(_testFilesDirectory, recursive: true);
			}
		}
	}
}