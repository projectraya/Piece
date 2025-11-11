using System.ComponentModel.DataAnnotations;

namespace Piece.Data.Models
{
	public class Playlist
	{
		public int Id { get; set; }

		[Required]
		[MaxLength(100)]
		public string Name { get; set; } = string.Empty;

		[MaxLength(500)]
		public string? Description { get; set; }

		[MaxLength(500)]
		public string? CoverImageUrl { get; set; }

		public bool IsPublic { get; set; } = true;

		[Required]
		public string UserId { get; set; } = string.Empty;
		public ApplicationUser User { get; set; } = null!;

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

		// Navigation properties
		public ICollection<PlaylistTrack> PlaylistTracks { get; set; } = new List<PlaylistTrack>();
	}
}
