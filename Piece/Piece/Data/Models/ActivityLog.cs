using System.ComponentModel.DataAnnotations;

namespace Piece.Data.Models
{
	public class ActivityLog
	{
		public int Id { get; set; }

		[Required]
		public string EventType { get; set; } = string.Empty; 

		[Required]
		public string Message { get; set; } = string.Empty;

		[Required]
		public string PerformedBy { get; set; } = string.Empty; 

		public string? TargetEntity { get; set; } 

		public string? AdditionalInfo { get; set; } 

		public string Severity { get; set; } = "Info";

		public DateTime Timestamp { get; set; } = DateTime.UtcNow;
	}
}
