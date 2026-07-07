namespace TicketingService.Clients.Catalog
{
    public sealed class CatalogGiftSnapshot
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Picture { get; set; } = string.Empty;
        public int Price { get; set; }
        public int? WinnerId { get; set; }
    }
}