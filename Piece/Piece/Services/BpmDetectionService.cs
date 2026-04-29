using NAudio.Wave;

namespace Piece.Services
{
	public static class BpmDetectionService
	{
		public static int DetectBpm(string filePath)
		{
			try
			{
				using var reader = new NAudio.Wave.Mp3FileReader(filePath);
				var sampleProvider = reader.ToSampleProvider();

				// Вземи моно канал
				var mono = sampleProvider.ToMono();

				// Събери сэмпли за анализ (първите 60 секунди)
				int sampleRate = mono.WaveFormat.SampleRate;
				int samplesToRead = Math.Min(sampleRate * 60, (int)(reader.TotalTime.TotalSeconds * sampleRate));
				var buffer = new float[samplesToRead];
				mono.Read(buffer, 0, samplesToRead);

				// Onset detection — намери енергийните пикове
				int windowSize = sampleRate / 10; // 100ms прозорец
				var energies = new List<float>();

				for (int i = 0; i < buffer.Length - windowSize; i += windowSize)
				{
					float energy = 0;
					for (int j = i; j < i + windowSize; j++)
						energy += buffer[j] * buffer[j];
					energies.Add(energy / windowSize);
				}

				// Намери beats — моменти с висока енергия
				float avgEnergy = energies.Average();
				float threshold = avgEnergy * 1.5f;

				var beatTimes = new List<float>();
				bool wasAbove = false;
				for (int i = 0; i < energies.Count; i++)
				{
					if (energies[i] > threshold && !wasAbove)
					{
						beatTimes.Add(i * 0.1f); // в секунди
						wasAbove = true;
					}
					else if (energies[i] <= threshold)
					{
						wasAbove = false;
					}
				}

				if (beatTimes.Count < 4)
					return 128; // fallback

				// Изчисли средния интервал между beats
				var intervals = new List<float>();
				for (int i = 1; i < beatTimes.Count; i++)
					intervals.Add(beatTimes[i] - beatTimes[i - 1]);

				// Филтрирай outliers
				intervals.Sort();
				var trimmed = intervals
					.Skip(intervals.Count / 10)
					.Take(intervals.Count * 8 / 10)
					.ToList();

				if (!trimmed.Any())
					return 128;

				float avgInterval = trimmed.Average();
				int bpm = (int)Math.Round(60f / avgInterval);

				// Коригирай ако е half/double time
				if (bpm < 60) bpm *= 2;
				if (bpm > 200) bpm /= 2;
				if (bpm < 60 || bpm > 200) return 128;

				Console.WriteLine($"[BpmDetection] Detected BPM: {bpm}");
				return bpm;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[BpmDetection] Error: {ex.Message}");
				return 128; // fallback
			}
		}
	}
}