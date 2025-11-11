using System.ComponentModel.DataAnnotations;

namespace Piece.Data.Models
{
	public class Country
	{
		public int Id { get; set; }

		[Required]
		[MaxLength(100)]
		public string Name { get; set; } = string.Empty;

		[Required]
		[MaxLength(3)]
		public string CountryCode { get; set; } = string.Empty;

		[MaxLength(2000)]
		public string? Description { get; set; }

		[MaxLength(500)]
		public string? FlagImageUrl { get; set; }

		// Map coordinates for clickable regions
		public double Latitude { get; set; }
		public double Longitude { get; set; }

		// Navigation properties
		public ICollection<Artist> Artists { get; set; } = new List<Artist>();
	}
}
