namespace Piece.Services
{
	public class UploadRateLimiter
	{
		private static readonly Dictionary<string, List<DateTime>> _uploadAttempts = new();
		private static readonly object _lock = new();

		private const int MaxUploadsPerHour = 50;
		private const int MaxFailedAttemptsPerHour = 10;

		public static bool CanUpload(string userId)
		{
			lock (_lock)
			{
				CleanupOldEntries(userId);

				if (!_uploadAttempts.ContainsKey(userId))
					return true;

				var recentAttempts = _uploadAttempts[userId]
					.Where(time => time > DateTime.UtcNow.AddHours(-1))
					.Count();

				return recentAttempts < MaxUploadsPerHour;
			}
		}

		public static void RecordUploadAttempt(string userId)
		{
			lock (_lock)
			{
				if (!_uploadAttempts.ContainsKey(userId))
					_uploadAttempts[userId] = new List<DateTime>();

				_uploadAttempts[userId].Add(DateTime.UtcNow);
				CleanupOldEntries(userId);
			}
		}

		public static bool HasTooManyFailedAttempts(string userId)
		{
			lock (_lock)
			{
				if (!_uploadAttempts.ContainsKey(userId))
					return false;

				var recentAttempts = _uploadAttempts[userId]
					.Where(time => time > DateTime.UtcNow.AddHours(-1))
					.Count();

				return recentAttempts >= MaxFailedAttemptsPerHour;
			}
		}

		private static void CleanupOldEntries(string userId)
		{
			if (!_uploadAttempts.ContainsKey(userId))
				return;

			var oneHourAgo = DateTime.UtcNow.AddHours(-1);
			_uploadAttempts[userId] = _uploadAttempts[userId]
				.Where(time => time > oneHourAgo)
				.ToList();

			if (!_uploadAttempts[userId].Any())
				_uploadAttempts.Remove(userId);
		}
	}
}