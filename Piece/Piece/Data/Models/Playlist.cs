using System.ComponentModel.DataAnnotations;

namespace Piece.Data.Models
{
	public class Playlist
	{
		public int Id { get; set; }

		[Required]
		[StringLength(100)]
		public string Name { get; set; } = string.Empty;

		[StringLength(500)]
		public string? Description { get; set; }

		public bool IsPublic { get; set; } = true;

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

		// Foreign key
		[Required]
		public string UserId { get; set; } = string.Empty;

		// Navigation properties
		public ApplicationUser User { get; set; } = null!;
		public ICollection<PlaylistTrack> PlaylistTracks { get; set; } = new List<PlaylistTrack>();
	}
}
