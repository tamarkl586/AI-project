using System.ComponentModel.DataAnnotations;

namespace TicketingService.Models
{
    public class Cart
    {
        public int Id { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        public int GiftID { get; set; }
        public Gift Gift { get; set; } = null!;

        [Required]
        [Range(1, 100)]
        public int Quantity { get; set; } = 1;

        public bool IsPurchased { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? PurchasedAt { get; set; }
    }
}
