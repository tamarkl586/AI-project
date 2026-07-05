namespace TicketingService.DTOs.Cart
{
    public class CartItemDTO
    {
        public int Id { get; set; } // (מזהה השורה בסל (למקרה שנרצה למחוק
        public int GiftId { get; set; }
        public string GiftName { get; set; } = string.Empty;
        public string GiftDescription { get; set; } = string.Empty;
        public string GiftPicture { get; set; } = string.Empty;
        public int Price { get; set; }
        public int Quantity { get; set; }
        public int TotalPrice { get; set; } // (שדה מחושב (כמות * מחיר
        public bool IsDrawn { get; set; } // האם כבר בוצעה הגרלה למתנה זו
    }
}