using System.IO;

namespace Piece.Services
{
	public static class FileValidator
	{
		private static readonly string[] AllowedAudioExtensions = { ".mp3", ".wav", ".flac", ".m4a" };
		private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

		public static bool IsValidAudioFile(string fileName, Stream fileStream)
		{
			var extension = Path.GetExtension(fileName).ToLowerInvariant();

			return AllowedAudioExtensions.Contains(extension);
		}

		public static bool IsValidImageFile(string fileName, Stream fileStream)
		{
			var extension = Path.GetExtension(fileName).ToLowerInvariant();

			return AllowedImageExtensions.Contains(extension);
		}

		public static string SanitizeFileName(string fileName)
		{
			fileName = Path.GetFileName(fileName);

			var invalidChars = Path.GetInvalidFileNameChars();
			foreach (var c in invalidChars)
			{
				fileName = fileName.Replace(c.ToString(), "");
			}

			return fileName;
		}
	}
}