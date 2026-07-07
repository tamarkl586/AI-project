using TicketingService.Models;

namespace TicketingService.DAL.Interfaces;

public interface IPurchaseRequestDAL
{
    Task AddRangeAsync(IEnumerable<TicketPurchaseRequest> requests);
    Task<TicketPurchaseRequest?> GetByIdAsync(Guid id);
    Task UpdateAsync(TicketPurchaseRequest request);
}
