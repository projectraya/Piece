using System.ComponentModel.DataAnnotations;

namespace Piece.Data.Models
{
	public class ListeningHistory
	{
		public int Id { get; set; }

		[Required]
		public string UserId { get; set; } = string.Empty;
		public ApplicationUser User { get; set; } = null!;

		public int TrackId { get; set; }
		public Track Track { get; set; } = null!;

		public DateTime PlayedAt { get; set; } = DateTime.UtcNow;

		// Duration in seconds - how long they actually listened
		public int DurationListened { get; set; }
	}
}
