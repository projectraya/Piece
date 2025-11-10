namespace Piece.Data.Models
{
	public class PlayHistory
	{
		public int Id { get; set; }

		// Foreign keys
		public string UserId { get; set; } = string.Empty;
		public int TrackId { get; set; }

		public DateTime PlayedAt { get; set; } = DateTime.UtcNow;

		// Navigation properties
		public ApplicationUser User { get; set; } = null!;
		public Track Track { get; set; } = null!;
	}
}
