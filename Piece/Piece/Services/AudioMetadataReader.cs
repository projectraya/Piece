using NAudio.Wave;

namespace Piece.Services
{
	public static class AudioMetadataReader
	{
		public static int GetDurationSeconds(string filePath)
		{
			Console.WriteLine($"[AudioMetadataReader] Attempting to read: {filePath}");
			Console.WriteLine($"[AudioMetadataReader] File exists: {File.Exists(filePath)}");

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
				Console.WriteLine($"[AudioMetadataReader] ERROR Type: {ex.GetType().Name}");
				Console.WriteLine($"[AudioMetadataReader] ERROR Message: {ex.Message}");
				Console.WriteLine($"[AudioMetadataReader] ERROR StackTrace: {ex.StackTrace}");

				if (ex.InnerException != null)
				{
					Console.WriteLine($"[AudioMetadataReader] INNER EXCEPTION: {ex.InnerException.Message}");
				}

				return 0;
			}
		}
	}
}