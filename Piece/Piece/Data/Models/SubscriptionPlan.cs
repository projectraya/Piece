using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Piece.Data.Models
{
	public class SubscriptionPlan
	{
		public int Id { get; set; }

		[Required]
		[MaxLength(50)]
		public string Name { get; set; } = string.Empty;

		[MaxLength(500)]
		public string? Description { get; set; }

		[Column(TypeName = "decimal(10,2)")]
		public decimal Price { get; set; }

		public int DurationDays { get; set; } = 30;

		// Features
		public bool CanSkipAds { get; set; } = false;
		public bool CanDownload { get; set; } = false;
		public bool HighQualityAudio { get; set; } = false;
		public int MaxDevices { get; set; } = 1;

		public bool IsActive { get; set; } = true;

		// Navigation properties
		public ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
	}
}
