namespace Piece.Services
{
	public static class SecurityLogger
	{
		public static void LogUploadAttempt(string userId, string fileName, bool success, string? reason = null)
		{
			var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
			var status = success ? "SUCCESS" : "FAILED";
			var message = $"[{timestamp}] UPLOAD {status} - User: {userId}, File: {fileName}";

			if (!string.IsNullOrEmpty(reason))
				message += $", Reason: {reason}";

			Console.WriteLine($"[SECURITY] {message}");

			// TODO: In production, write to a secure log file or logging service
		}

		public static void LogSuspiciousActivity(string userId, string activity, string details)
		{
			var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
			Console.WriteLine($"[SECURITY ALERT] [{timestamp}] User: {userId}, Activity: {activity}, Details: {details}");

			// TODO: In production, send alerts to admin or monitoring system
		}

		public static void LogRateLimitExceeded(string userId)
		{
			var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
			Console.WriteLine($"[SECURITY ALERT] [{timestamp}] RATE LIMIT EXCEEDED - User: {userId}");
		}
	}
}