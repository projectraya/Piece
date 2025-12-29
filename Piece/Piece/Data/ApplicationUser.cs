using Microsoft.AspNetCore.Identity;
using Piece.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace Piece.Data
{
	
	public class ApplicationUser : IdentityUser
	{
		[MaxLength(100)]
		public string? DisplayName { get; set; }

		[MaxLength(500)]
		public string? Bio { get; set; }

		[MaxLength(500)]
		public string? AvatarUrl { get; set; }
		public bool IsProfilePublic { get; set; } = true;
		public bool ShowListeningHistory { get; set; } = true;
		public bool ShowPlaylists { get; set; } = true;

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime? LastActiveAt { get; set; }

		public bool IsAdmin { get; set; } = false;

		public bool IsPremium { get; set; } = false;

		// Navigation properties
		public ICollection<Playlist> Playlists { get; set; } = new List<Playlist>();
		public ICollection<UserFavorites> LikedTracks { get; set; } = new List<UserFavorites>();
		public ICollection<PlayHistory> PlayHistory { get; set; } = new List<PlayHistory>();
		public ICollection<UserSubscription> Subscriptions { get; set; } = new List<UserSubscription>();
		public ICollection<Payment> Payments { get; set; } = new List<Payment>();
	}

}
