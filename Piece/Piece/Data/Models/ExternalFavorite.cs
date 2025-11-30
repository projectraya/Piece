using Piece.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace Piece.Data.Models
{
	public class ExternalFavorite
	{
		public int Id { get; set; }

		[Required]
		public string UserId { get; set; } = string.Empty;
		public ApplicationUser User { get; set; } = null!;

		[Required]
		public TrackSource Source { get; set; } // Jamendo, Spotify, etc.

		[Required]
		public string ExternalId { get; set; } = string.Empty; // The Jamendo track ID

		// Minimal metadata for display
		public string Title { get; set; } = string.Empty;
		public string ArtistName { get; set; } = string.Empty;
		public string? AlbumImage { get; set; }
		public string AudioUrl { get; set; } = string.Empty;

		public DateTime LikedAt { get; set; } = DateTime.UtcNow;
	}
}
