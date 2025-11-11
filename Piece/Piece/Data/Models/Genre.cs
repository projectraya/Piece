using System.ComponentModel.DataAnnotations;

namespace Piece.Data.Models
{
	public class Genre
	{
		public int Id { get; set; }

		[Required]
		[MaxLength(50)]
		public string Name { get; set; } = string.Empty;

		[MaxLength(500)]
		public string? Description { get; set; }

		// Navigation properties
		public ICollection<Track> Tracks { get; set; } = new List<Track>();
	}
}
