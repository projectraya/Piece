using Humanizer.Localisation;
using Piece.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace Piece.Data.Models
{
	public class Track
	{
		public int Id { get; set; }

		[Required]
		[MaxLength(200)]
		public string Title { get; set; } = string.Empty;

		[Required]
		[MaxLength(200)]
		public string ArtistName { get; set; } = string.Empty;

		[MaxLength(200)]
		public string AlbumName { get; set; } = string.Empty;

		// Genre relationship
		public int? GenreId { get; set; }
		public Genre? Genre { get; set; }

		public int? YearPublished { get; set; }

		public int DurationSeconds { get; set; }

		public int PlayCount { get; set; } = 0;

		[MaxLength(500)]
		public string? CoverImageUrl { get; set; }

		// Hybrid system: Local files OR Jamendo streaming
		public TrackSource Source { get; set; }

		// For LOCAL tracks only
		[MaxLength(500)]
		public string? LocalFilePath { get; set; }

		// For JAMENDO tracks only
		[MaxLength(100)]
		public string? JamendoTrackId { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		public bool IsActive { get; set; } = true;

		// Navigation properties
		public ICollection<PlaylistTrack> PlaylistTracks { get; set; } = new List<PlaylistTrack>();
		public ICollection<UserFavorites> UserTrackLikes { get; set; } = new List<UserFavorites>();
		public ICollection<PlayHistory> PlayHistories { get; set; } = new List<PlayHistory>();
	}

}
