using System.ComponentModel.DataAnnotations;

namespace Piece.Data.Models
{
	/// <summary>
	/// Represents a track associated with an Artist on the map feature.
	/// These tracks have 30-second preview URLs from Spotify.
	/// This is SEPARATE from the main Track entity used in playlists.
	/// </summary>
	public class ArtistTrack
	{
		public int Id { get; set; }

		public int ArtistId { get; set; }
		public Artist Artist { get; set; } = null!;

		[Required]
		[MaxLength(200)]
		public string TrackName { get; set; } = string.Empty;

		[MaxLength(200)]
		public string AlbumName { get; set; } = string.Empty;

		public int? YearReleased { get; set; }

		public int DurationSeconds { get; set; }

		// 30-second preview URL from Spotify API
		[MaxLength(500)]
		public string? PreviewUrl { get; set; }

		[MaxLength(100)]
		public string? SpotifyTrackId { get; set; }

		[MaxLength(500)]
		public string? AlbumArtUrl { get; set; }

		public int Popularity { get; set; } = 0;

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	}
}
