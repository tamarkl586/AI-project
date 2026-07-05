using System.ComponentModel.DataAnnotations;

namespace TicketingService.DTOs.Cart
{
    public class AddToCartDTO
    {
        [Required(ErrorMessage = "חובה לבחור מתנה")]
        public int? GiftId { get; set; } 

        [Range(1, 100, ErrorMessage = "כמות חייבת להיות בין 1 ל-100")]
        public int Quantity { get; set; } = 1;
    }
}