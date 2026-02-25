using NAudio.Wave;
using TagLib;

namespace Piece.Services
{
	public static class AudioMetadataReader
	{
		public static int GetDurationSeconds(string filePath)
		{
			Console.WriteLine($"[AudioMetadataReader] Attempting to read: {filePath}");
			Console.WriteLine($"[AudioMetadataReader] File exists: {System.IO.File.Exists(filePath)}");
			try
			{
				using (var reader = new Mp3FileReader(filePath))
				{
					var duration = (int)reader.TotalTime.TotalSeconds;
					Console.WriteLine($"[AudioMetadataReader] SUCCESS - Duration: {duration}s");
					return duration;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[AudioMetadataReader] ERROR: {ex.Message}");
				return 0;
			}
		}

		public static int? GetYearPublished(string filePath)
		{
			try
			{
				var tagFile = TagLib.File.Create(filePath);
				if (tagFile.Tag.Year > 0)
					return (int)tagFile.Tag.Year;
				return null;
			}
			catch
			{
				return null;
			}
		}
	}
}