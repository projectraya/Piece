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
		private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

		public ActivityLogger(IDbContextFactory<ApplicationDbContext> dbFactory)
		{
			_dbFactory = dbFactory;
		}


		public async Task LogAsync(string eventType, string message, string performedBy, string? targetEntity = null, string? additionalInfo = null, string severity = "Info")
		{
			using var context = await _dbFactory.CreateDbContextAsync();
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

			context.ActivityLogs.Add(log);
			await context.SaveChangesAsync();

			Console.WriteLine($"[ActivityLogger] {severity}: {message}");
		}

		public async Task<List<ActivityLog>> GetRecentLogsAsync(int count = 100)
		{
			using var context = await _dbFactory.CreateDbContextAsync();
			return await context.ActivityLogs
				.OrderByDescending(l => l.Timestamp)
				.Take(count)
				.ToListAsync();
		}

		public async Task<List<ActivityLog>> GetFilteredLogsAsync(string? eventType = null, DateTime? startDate = null, DateTime? endDate = null)
		{
			using var context = await _dbFactory.CreateDbContextAsync();
			var query = context.ActivityLogs.AsQueryable();

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