using Microsoft.EntityFrameworkCore;
using TripPacking.Data;
using TripPacking.DTOs;
using TripPacking.Entities;

namespace TripPacking.Repositories;

public class TripRepository : IRepository<Trip>, ITripRepository
{
    private readonly AppDbContext _context;

    public TripRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Trip>> GetAllAsync()
    {
        return await _context.Set<Trip>()
            .Include(t => t.Owner)
            .Include(t => t.TripMembers)
                .ThenInclude(tm => tm.User)
            .ToListAsync();
    }

    public async Task<Trip?> GetByIdAsync(int id)
    {
        return await _context.Set<Trip>()
            .Include(t => t.Owner)
            .Include(t => t.TripMembers)
                .ThenInclude(tm => tm.User)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task AddAsync(Trip entity)
    {
        await _context.Set<Trip>().AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Trip entity)
    {
        _context.Set<Trip>().Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Trip entity)
    {
        _context.Set<Trip>().Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Trip>> GetByUserIdAsync(int userId)
    {
        return await _context.Set<Trip>()
            .Include(t => t.Owner)
            .Include(t => t.TripMembers)
                .ThenInclude(tm => tm.User)
            .Where(t => t.OwnerId == userId || t.TripMembers.Any(tm => tm.UserId == userId))
            .ToListAsync();
    }

    public async Task<IEnumerable<Trip>> GetByOwnerIdAsync(int ownerId)
    {
        return await _context.Set<Trip>()
            .Include(t => t.Owner)
            .Include(t => t.TripMembers)
                .ThenInclude(tm => tm.User)
            .Where(t => t.OwnerId == ownerId)
            .ToListAsync();
    }

    public async Task<PagedResult<Trip>> GetPagedAsync(int pageIndex, int pageSize, string? keyword, TripStatus? status, int? userId = null)
    {
        var query = _context.Set<Trip>()
            .Include(t => t.Owner)
            .Include(t => t.TripMembers)
                .ThenInclude(tm => tm.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(t => t.Title.Contains(keyword) || t.Destination.Contains(keyword));

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (userId.HasValue)
            query = query.Where(t => t.OwnerId == userId.Value || t.TripMembers.Any(tm => tm.UserId == userId.Value));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Trip> { Items = items, Total = total };
    }
}
