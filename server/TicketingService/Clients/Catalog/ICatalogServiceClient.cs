namespace TicketingService.Clients.Catalog
{
    public interface ICatalogServiceClient
    {
        Task<CatalogGiftSnapshot?> GetGiftByIdAsync(int giftId, CancellationToken cancellationToken = default);
    }
}