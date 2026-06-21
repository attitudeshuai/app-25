using Microsoft.EntityFrameworkCore;
using TripPacking.Data;
using TripPacking.DTOs;
using TripPacking.Entities;

namespace TripPacking.Repositories;

public class InvitationRepository : IRepository<Invitation>, IInvitationRepository
{
    private readonly AppDbContext _context;

    public InvitationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Invitation>> GetAllAsync()
    {
        return await _context.Set<Invitation>()
            .Include(i => i.Trip)
            .Include(i => i.InvitedBy)
            .Include(i => i.InvitedUser)
            .ToListAsync();
    }

    public async Task<Invitation?> GetByIdAsync(int id)
    {
        return await _context.Set<Invitation>()
            .Include(i => i.Trip)
            .Include(i => i.InvitedBy)
            .Include(i => i.InvitedUser)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task AddAsync(Invitation entity)
    {
        await _context.Set<Invitation>().AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Invitation entity)
    {
        _context.Set<Invitation>().Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Invitation entity)
    {
        _context.Set<Invitation>().Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Invitation>> GetByTripIdAsync(int tripId)
    {
        return await _context.Set<Invitation>()
            .Include(i => i.Trip)
            .Include(i => i.InvitedBy)
            .Include(i => i.InvitedUser)
            .Where(i => i.TripId == tripId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Invitation>> GetByInvitedUserIdAsync(int userId)
    {
        return await _context.Set<Invitation>()
            .Include(i => i.Trip)
            .Include(i => i.InvitedBy)
            .Include(i => i.InvitedUser)
            .Where(i => i.InvitedUserId == userId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Invitation>> GetByInvitedByIdAsync(int userId)
    {
        return await _context.Set<Invitation>()
            .Include(i => i.Trip)
            .Include(i => i.InvitedBy)
            .Include(i => i.InvitedUser)
            .Where(i => i.InvitedById == userId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<Invitation?> GetPendingByTripAndUserAsync(int tripId, int invitedUserId)
    {
        return await _context.Set<Invitation>()
            .Include(i => i.Trip)
            .Include(i => i.InvitedBy)
            .Include(i => i.InvitedUser)
            .FirstOrDefaultAsync(i =>
                i.TripId == tripId &&
                i.InvitedUserId == invitedUserId &&
                i.Status == InvitationStatus.Pending);
    }

    public async Task<PagedResult<Invitation>> GetPagedAsync(int pageIndex, int pageSize, int? tripId, InvitationStatus? status, string? direction, int currentUserId)
    {
        var query = _context.Set<Invitation>()
            .Include(i => i.Trip)
            .Include(i => i.InvitedBy)
            .Include(i => i.InvitedUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(direction))
        {
            if (direction.Equals("sent", StringComparison.OrdinalIgnoreCase))
                query = query.Where(i => i.InvitedById == currentUserId);
            else if (direction.Equals("received", StringComparison.OrdinalIgnoreCase))
                query = query.Where(i => i.InvitedUserId == currentUserId);
        }
        else
        {
            query = query.Where(i => i.InvitedById == currentUserId || i.InvitedUserId == currentUserId);
        }

        if (tripId.HasValue)
            query = query.Where(i => i.TripId == tripId.Value);

        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Invitation> { Items = items, Total = total };
    }

    public async Task<IEnumerable<Invitation>> GetExpiredPendingAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.Set<Invitation>()
            .Include(i => i.Trip)
            .Include(i => i.InvitedBy)
            .Include(i => i.InvitedUser)
            .Where(i => i.Status == InvitationStatus.Pending && i.ExpiresAt <= now)
            .ToListAsync();
    }
}
