using Piece.Data;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Security.Cryptography;

namespace Piece.Services
{
	public static class FileValidator
	{
		private static readonly string[] AllowedAudioExtensions = { ".mp3", ".wav", ".flac", ".m4a" };
		private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

		private const int MaxFileNameLength = 255;

		// Validates audio file extension only (magic bytes checked after file is saved)

		public static bool IsValidAudioExtension(string fileName)
		{
			var extension = Path.GetExtension(fileName).ToLowerInvariant();
			return AllowedAudioExtensions.Contains(extension);
		}

		// Validates audio file by checking magic bytes from saved file
		public static bool ValidateAudioFileMagicBytes(string filePath)
		{
			try
			{
				using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
				{
					byte[] headerBytes = new byte[10];
					int bytesRead = fileStream.Read(headerBytes, 0, 10);

					if (bytesRead < 3)
						return false;

					// MP3 magic bytes
					// ID3v2 tag: 49 44 33 (ID3)
					if (headerBytes[0] == 0x49 && headerBytes[1] == 0x44 && headerBytes[2] == 0x33)
						return true;

					// MPEG audio frame sync: FF Fx
					if (headerBytes[0] == 0xFF && (headerBytes[1] & 0xE0) == 0xE0)
						return true;

					// WAV: 52 49 46 46 (RIFF)
					if (headerBytes[0] == 0x52 && headerBytes[1] == 0x49 &&
						headerBytes[2] == 0x46 && headerBytes[3] == 0x46)
						return true;

					// FLAC: 66 4C 61 43 (fLaC)
					if (headerBytes[0] == 0x66 && headerBytes[1] == 0x4C &&
						headerBytes[2] == 0x61 && headerBytes[3] == 0x43)
						return true;

					return false;
				}
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Validates image file extension only
		/// </summary>
		public static bool IsValidImageExtension(string fileName)
		{
			var extension = Path.GetExtension(fileName).ToLowerInvariant();
			return AllowedImageExtensions.Contains(extension);
		}

		/// <summary>
		/// Validates image file by checking magic bytes from saved file
		/// </summary>
		public static bool ValidateImageFileMagicBytes(string filePath)
		{
			try
			{
				using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
				{
					byte[] headerBytes = new byte[8];
					int bytesRead = fileStream.Read(headerBytes, 0, 8);

					if (bytesRead < 4)
						return false;

					// PNG: 89 50 4E 47
					if (headerBytes[0] == 0x89 && headerBytes[1] == 0x50 &&
						headerBytes[2] == 0x4E && headerBytes[3] == 0x47)
						return true;

					// JPEG: FF D8 FF
					if (headerBytes[0] == 0xFF && headerBytes[1] == 0xD8 && headerBytes[2] == 0xFF)
						return true;

					// WEBP: 52 49 46 46
					if (headerBytes[0] == 0x52 && headerBytes[1] == 0x49 &&
						headerBytes[2] == 0x46 && headerBytes[3] == 0x46)
						return true;

					return false;
				}
			}
			catch
			{
				return false;
			}
		}
		/// <summary>
		/// Checks if a track with the same title and artist already exists
		/// </summary>
		public static async Task<bool> IsDuplicateTrack(ApplicationDbContext dbContext, string title, string artistName)
		{
			return await dbContext.Tracks
				.AnyAsync(t =>
					t.Title.ToLower() == title.ToLower() &&
					t.ArtistName.ToLower() == artistName.ToLower() &&
					t.IsActive);
		}
		/// <summary>
		/// Sanitizes filename and ensures it's not too long
		/// </summary>
		public static string SanitizeFileName(string fileName)
		{
			if (string.IsNullOrWhiteSpace(fileName))
				return "unnamed";

			// Remove path traversal attempts
			fileName = Path.GetFileName(fileName);

			// Remove invalid characters
			var invalidChars = Path.GetInvalidFileNameChars();
			foreach (var c in invalidChars)
			{
				fileName = fileName.Replace(c.ToString(), "");
			}

			// Remove potentially dangerous characters
			fileName = fileName.Replace("$", "")
							   .Replace("`", "")
							   .Replace("{", "")
							   .Replace("}", "")
							   .Replace("[", "")
							   .Replace("]", "");

			// Limit length
			if (fileName.Length > MaxFileNameLength)
			{
				var extension = Path.GetExtension(fileName);
				var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
				fileName = nameWithoutExt.Substring(0, MaxFileNameLength - extension.Length) + extension;
			}

			return fileName;
		}

		/// <summary>
		/// Check if disk has enough free space (at least 200MB)
		/// </summary>
		public static bool HasSufficientDiskSpace(string path)
		{
			try
			{
				var drive = new DriveInfo(Path.GetPathRoot(path) ?? "C:\\");
				const long minimumFreeSpace = 200L * 1024 * 1024; // 200MB
				return drive.AvailableFreeSpace > minimumFreeSpace;
			}
			catch
			{
				return true; // If we can't check, allow the operation
			}
		}


		/// <summary>
		/// Calculates SHA256 hash of a file
		/// </summary>
		public static string CalculateFileHash(string filePath)
		{
			try
			{
				using (var sha256 = SHA256.Create())
				using (var stream = File.OpenRead(filePath))
				{
					var hash = sha256.ComputeHash(stream);
					return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[FileValidator] Error calculating hash: {ex.Message}");
				return string.Empty;
			}
		}

		/// <summary>
		/// Checks if a file with the same hash already exists
		/// </summary>
		public static async Task<(bool exists, string? existingTitle)> IsDuplicateFile(ApplicationDbContext dbContext, string fileHash)
		{
			var existingTrack = await dbContext.Tracks
				.Where(t => t.FileHash == fileHash && t.IsActive)
				.Select(t => new { t.Title, t.ArtistName })
				.FirstOrDefaultAsync();

			if (existingTrack != null)
			{
				return (true, $"{existingTrack.Title} by {existingTrack.ArtistName}");
			}

			return (false, null);
		}
	}
}
