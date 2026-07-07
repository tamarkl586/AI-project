using Microsoft.EntityFrameworkCore;
using TicketingService.DAL.Interfaces;
using TicketingService.Models;

namespace TicketingService.DAL.Implementations;

public class PurchaseRequestDAL : IPurchaseRequestDAL
{
    private readonly TicketingDbContext _context;

    public PurchaseRequestDAL(TicketingDbContext context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(IEnumerable<TicketPurchaseRequest> requests)
    {
        await _context.TicketPurchaseRequests.AddRangeAsync(requests);
        await _context.SaveChangesAsync();
    }

    public Task<TicketPurchaseRequest?> GetByIdAsync(Guid id)
    {
        return _context.TicketPurchaseRequests.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task UpdateAsync(TicketPurchaseRequest request)
    {
        _context.TicketPurchaseRequests.Update(request);
        await _context.SaveChangesAsync();
    }
}
