using Microsoft.EntityFrameworkCore;
using TripPacking.Data;
using TripPacking.Entities;

namespace TripPacking.Repositories;

public class TripStatusHistoryRepository : ITripStatusHistoryRepository
{
    private readonly AppDbContext _context;

    public TripStatusHistoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(TripStatusHistory history)
    {
        await _context.TripStatusHistories.AddAsync(history);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<TripStatusHistory>> GetByTripIdAsync(int tripId)
    {
        return await _context.TripStatusHistories
            .Include(h => h.ChangedByUser)
            .Where(h => h.TripId == tripId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync();
    }
}
