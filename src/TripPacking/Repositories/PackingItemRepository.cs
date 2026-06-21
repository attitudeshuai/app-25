using Microsoft.EntityFrameworkCore;
using TripPacking.Data;
using TripPacking.DTOs;
using TripPacking.Entities;

namespace TripPacking.Repositories;

public class PackingItemRepository : IRepository<PackingItem>, IPackingItemRepository
{
    private readonly AppDbContext _context;

    public PackingItemRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PackingItem>> GetAllAsync()
    {
        return await _context.Set<PackingItem>()
            .Include(pi => pi.Trip)
                .ThenInclude(t => t.TripMembers)
            .Include(pi => pi.Category)
            .Include(pi => pi.AssignedUser)
            .ToListAsync();
    }

    public async Task<PackingItem?> GetByIdAsync(int id)
    {
        return await _context.Set<PackingItem>()
            .Include(pi => pi.Trip)
                .ThenInclude(t => t.TripMembers)
            .Include(pi => pi.Category)
            .Include(pi => pi.AssignedUser)
            .FirstOrDefaultAsync(pi => pi.Id == id);
    }

    public async Task AddAsync(PackingItem entity)
    {
        await _context.Set<PackingItem>().AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(PackingItem entity)
    {
        _context.Set<PackingItem>().Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(PackingItem entity)
    {
        _context.Set<PackingItem>().Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<PackingItem>> GetByTripIdAsync(int tripId)
    {
        return await _context.Set<PackingItem>()
            .Include(pi => pi.Trip)
                .ThenInclude(t => t.TripMembers)
            .Include(pi => pi.Category)
            .Include(pi => pi.AssignedUser)
            .Where(pi => pi.TripId == tripId)
            .ToListAsync();
    }

    public async Task<PagedResult<PackingItem>> GetPagedAsync(int pageIndex, int pageSize, string? keyword, int? tripId, int? categoryId, bool? isPacked, bool? isShared, int? currentUserId = null)
    {
        var query = _context.Set<PackingItem>()
            .Include(pi => pi.Trip)
                .ThenInclude(t => t.TripMembers)
            .Include(pi => pi.Category)
            .Include(pi => pi.AssignedUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(pi => pi.Name.Contains(keyword));

        if (tripId.HasValue)
            query = query.Where(pi => pi.TripId == tripId.Value);

        if (categoryId.HasValue)
            query = query.Where(pi => pi.CategoryId == categoryId.Value);

        if (isPacked.HasValue)
            query = query.Where(pi => pi.IsPacked == isPacked.Value);

        if (isShared.HasValue)
            query = query.Where(pi => pi.IsShared == isShared.Value);

        if (currentUserId.HasValue)
            query = query.Where(pi => pi.Trip.OwnerId == currentUserId.Value || pi.Trip.TripMembers.Any(tm => tm.UserId == currentUserId.Value));

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(pi => pi.Category.SortOrder)
            .ThenBy(pi => pi.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<PackingItem> { Items = items, Total = total };
    }
}
