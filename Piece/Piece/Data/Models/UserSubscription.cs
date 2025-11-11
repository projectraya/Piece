using Piece.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace Piece.Data.Models
{
	public class UserSubscription
	{
		public int Id { get; set; }

		[Required]
		public string UserId { get; set; } = string.Empty;
		public ApplicationUser User { get; set; } = null!;

		public int SubscriptionPlanId { get; set; }
		public SubscriptionPlan SubscriptionPlan { get; set; } = null!;

		public DateTime StartDate { get; set; } = DateTime.UtcNow;
		public DateTime EndDate { get; set; }

		public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

		public bool AutoRenew { get; set; } = true;

		[MaxLength(100)]
		public string? PaymentMethod { get; set; }

		[MaxLength(200)]
		public string? TransactionId { get; set; }

		// Navigation properties
		public ICollection<Payment> Payments { get; set; } = new List<Payment>();
	}

	
}
