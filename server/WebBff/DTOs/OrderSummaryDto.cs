namespace WebBff.DTOs;

public sealed class WebOrderSummaryDto
{
    public int UserId { get; set; }
    public int? RequestedUserId { get; set; }
    public List<WebOrderItemDto> Items { get; set; } = [];
    public int TotalQuantity { get; set; }
    public int TotalPrice { get; set; }
    public DateTime RetrievedAtUtc { get; set; }
}

public sealed class WebOrderItemDto
{
    public TicketingCartItemDto CartItem { get; set; } = new();
    public CatalogGiftDto? Gift { get; set; }
}

public sealed class TicketingCartItemDto
{
    public int Id { get; set; }
    public int GiftId { get; set; }
    public string GiftName { get; set; } = string.Empty;
    public string GiftDescription { get; set; } = string.Empty;
    public string GiftPicture { get; set; } = string.Empty;
    public int Price { get; set; }
    public int Quantity { get; set; }
    public int TotalPrice { get; set; }
    public bool IsDrawn { get; set; }
}

public sealed class CatalogGiftDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Picture { get; set; } = string.Empty;
    public int Price { get; set; }
    public int DonorId { get; set; }
    public string DonorName { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? WinnerId { get; set; }
}
