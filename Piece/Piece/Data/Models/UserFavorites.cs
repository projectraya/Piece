using System.ComponentModel.DataAnnotations;

namespace Piece.Data.Models
{
	public class UserFavorites
	{
		public int Id { get; set; }

		[Required]
		public string UserId { get; set; } = string.Empty;
		public ApplicationUser User { get; set; } = null!;

		public int TrackId { get; set; }
		public Track Track { get; set; } = null!;

		public DateTime LikedAt { get; set; } = DateTime.UtcNow;
	}
}
