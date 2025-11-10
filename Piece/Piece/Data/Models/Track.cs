using Piece.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace Piece.Data.Models
{
	public class Track
	{
		public int Id { get; set; }

		[Required]
		[StringLength(200)]
		public string Title { get; set; } = string.Empty;

		[Required]
		[StringLength(200)]
		public string Artist { get; set; } = string.Empty;

		[StringLength(200)]
		public string Album { get; set; } = string.Empty;

		public int DurationSeconds { get; set; }

		[StringLength(100)]
		public string Genre { get; set; } = string.Empty;

		[StringLength(500)]
		public string? AlbumArtUrl { get; set; }

		// Source identification
		public TrackSource Source { get; set; }

		// For LOCAL tracks only
		[StringLength(500)]
		public string? LocalFilePath { get; set; }

		// For JAMENDO tracks only
		[StringLength(100)]
		public string? JamendoTrackId { get; set; }

		// Metadata
		public DateTime AddedAt { get; set; } = DateTime.UtcNow;
		public int PlayCount { get; set; } = 0;

		// Navigation properties
		public ICollection<PlaylistTrack> PlaylistTracks { get; set; } = new List<PlaylistTrack>();
		public ICollection<UserFavorites> LikedByUsers { get; set; } = new List<UserFavorites>();
		public ICollection<PlayHistory> PlayHistories { get; set; } = new List<PlayHistory>();
	}

}
