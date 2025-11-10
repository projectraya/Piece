namespace Piece.Data.Models
{
	public class PlaylistTrack
	{
		public int Id { get; set; }

		// Foreign keys
		public int PlaylistId { get; set; }
		public int TrackId { get; set; }

		// Position in playlist (for ordering)
		public int Position { get; set; }

		public DateTime AddedAt { get; set; } = DateTime.UtcNow;

		// Navigation properties
		public Playlist Playlist { get; set; } = null!;
		public Track Track { get; set; } = null!;
	}
}
