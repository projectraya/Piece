using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Piece.Data.Enums;

namespace Piece.Data.Models
{
	public class Payment
	{
		public int Id { get; set; }

		[Required]
		public string UserId { get; set; } = string.Empty;
		public ApplicationUser User { get; set; } = null!;

		public int? UserSubscriptionId { get; set; }
		public UserSubscription? UserSubscription { get; set; }

		[Column(TypeName = "decimal(10,2)")]
		public decimal Amount { get; set; }

		[MaxLength(3)]
		public string Currency { get; set; } = "USD";

		public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

		[Required]
		[MaxLength(100)]
		public string PaymentMethod { get; set; } = string.Empty;

		[MaxLength(200)]
		public string? TransactionId { get; set; }

		[MaxLength(2000)]
		public string? PaymentGatewayResponse { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime? CompletedAt { get; set; }
	}

	
}
