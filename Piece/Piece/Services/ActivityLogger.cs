using Microsoft.EntityFrameworkCore;
using Piece.Data;
using Piece.Data.Models;

namespace Piece.Services
{
	public interface IActivityLogger
	{
		Task LogAsync(string eventType, string message, string performedBy, string? targetEntity = null, string? additionalInfo = null, string severity = "Info");
		Task<List<ActivityLog>> GetRecentLogsAsync(int count = 100);
		Task<List<ActivityLog>> GetFilteredLogsAsync(string? eventType = null, DateTime? startDate = null, DateTime? endDate = null);
	}

	public class ActivityLogger : IActivityLogger
	{
		private readonly ApplicationDbContext _context;

		public ActivityLogger(ApplicationDbContext context)
		{
			_context = context;
		}

		public async Task LogAsync(string eventType, string message, string performedBy, string? targetEntity = null, string? additionalInfo = null, string severity = "Info")
		{
			var log = new ActivityLog
			{
				EventType = eventType,
				Message = message,
				PerformedBy = performedBy,
				TargetEntity = targetEntity,
				AdditionalInfo = additionalInfo,
				Severity = severity,
				Timestamp = DateTime.UtcNow
			};

			_context.ActivityLogs.Add(log);
			await _context.SaveChangesAsync();

			Console.WriteLine($"[ActivityLogger] {severity}: {message}");
		}

		public async Task<List<ActivityLog>> GetRecentLogsAsync(int count = 100)
		{
			return await _context.ActivityLogs
				.OrderByDescending(l => l.Timestamp)
				.Take(count)
				.ToListAsync();
		}

		public async Task<List<ActivityLog>> GetFilteredLogsAsync(string? eventType = null, DateTime? startDate = null, DateTime? endDate = null)
		{
			var query = _context.ActivityLogs.AsQueryable();

			if (!string.IsNullOrEmpty(eventType))
				query = query.Where(l => l.EventType == eventType);

			if (startDate.HasValue)
				query = query.Where(l => l.Timestamp >= startDate.Value);

			if (endDate.HasValue)
				query = query.Where(l => l.Timestamp <= endDate.Value);

			return await query
				.OrderByDescending(l => l.Timestamp)
				.ToListAsync();
		}
	}
}