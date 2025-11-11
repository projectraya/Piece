using Piece.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace Piece.Data.Models
{
	public class Artist
	{
		public int Id { get; set; }

		[Required]
		[MaxLength(200)]
		public string Name { get; set; } = string.Empty;

		[MaxLength(2000)]
		public string? Bio { get; set; }

		[MaxLength(500)]
		public string? ImageUrl { get; set; }

		[MaxLength(100)]
		public string Genre { get; set; } = string.Empty;

		// Geographic data - CRITICAL for map feature
		public int CountryId { get; set; }
		public Country Country { get; set; } = null!;

		// External API references
		public ArtistDataSource DataSource { get; set; }

		[MaxLength(100)]
		public string? MusicBrainzId { get; set; }

		[MaxLength(100)]
		public string? SpotifyId { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		// Navigation properties
		public ICollection<ArtistTrack> ArtistTracks { get; set; } = new List<ArtistTrack>();
	}

	
}
