using Microsoft.AspNetCore.Identity;
using Piece.Data.Models;

namespace Piece.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
		// Custom fields
		public string? DisplayName { get; set; }
		public string? Bio { get; set; }
		public string? AvatarUrl { get; set; }
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public bool IsAdmin { get; set; } = false;

		// Navigation properties
		public ICollection<Playlist> Playlists { get; set; } = new List<Playlist>();
		public ICollection<UserFavorites> LikedTracks { get; set; } = new List<UserFavorites>();
		public ICollection<PlayHistory> PlayHistory { get; set; } = new List<PlayHistory>();
	}

}
