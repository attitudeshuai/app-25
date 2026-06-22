using Microsoft.EntityFrameworkCore;
using TripPacking.Data;
using TripPacking.DTOs;
using TripPacking.Entities;

namespace TripPacking.Repositories;

public class PackingCategoryRepository : IRepository<PackingCategory>, IPackingCategoryRepository
{
    private readonly AppDbContext _context;

    public PackingCategoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PackingCategory>> GetAllAsync()
    {
        return await _context.Set<PackingCategory>()
            .Include(pc => pc.Trip)
                .ThenInclude(t => t.TripMembers)
            .Include(pc => pc.PackingItems)
            .ToListAsync();
    }

    public async Task<PackingCategory?> GetByIdAsync(int id)
    {
        return await _context.Set<PackingCategory>()
            .Include(pc => pc.Trip)
                .ThenInclude(t => t.TripMembers)
            .Include(pc => pc.PackingItems)
            .FirstOrDefaultAsync(pc => pc.Id == id);
    }

    public async Task AddAsync(PackingCategory entity)
    {
        await _context.Set<PackingCategory>().AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(PackingCategory entity)
    {
        _context.Set<PackingCategory>().Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(PackingCategory entity)
    {
        _context.Set<PackingCategory>().Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<PackingCategory>> GetByTripIdAsync(int tripId)
    {
        return await _context.Set<PackingCategory>()
            .Include(pc => pc.Trip)
                .ThenInclude(t => t.TripMembers)
            .Include(pc => pc.PackingItems)
            .Where(pc => pc.TripId == tripId)
            .OrderBy(pc => pc.SortOrder)
            .ToListAsync();
    }

    public async Task<PagedResult<PackingCategory>> GetPagedAsync(int pageIndex, int pageSize, string? keyword, int? tripId, int? currentUserId = null)
    {
        var query = _context.Set<PackingCategory>()
            .Include(pc => pc.Trip)
                .ThenInclude(t => t.TripMembers)
            .Include(pc => pc.PackingItems)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(pc => pc.Name.Contains(keyword));

        if (tripId.HasValue)
            query = query.Where(pc => pc.TripId == tripId.Value);

        if (currentUserId.HasValue)
            query = query.Where(pc => pc.Trip.OwnerId == currentUserId.Value || pc.Trip.TripMembers.Any(tm => tm.UserId == currentUserId.Value));

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(pc => pc.SortOrder)
            .ThenBy(pc => pc.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<PackingCategory> { Items = items, Total = total };
    }

    public async Task<bool> IsSortOrderDuplicateAsync(int tripId, int sortOrder, int? excludeCategoryId = null)
    {
        var query = _context.Set<PackingCategory>()
            .Where(pc => pc.TripId == tripId && pc.SortOrder == sortOrder);

        if (excludeCategoryId.HasValue)
            query = query.Where(pc => pc.Id != excludeCategoryId.Value);

        return await query.AnyAsync();
    }

    public async Task<IEnumerable<PackingCategory>> GetByIdsAsync(IEnumerable<int> ids)
    {
        return await _context.Set<PackingCategory>()
            .Where(pc => ids.Contains(pc.Id))
            .ToListAsync();
    }

    public async Task UpdateRangeAsync(IEnumerable<PackingCategory> categories)
    {
        _context.Set<PackingCategory>().UpdateRange(categories);
        await _context.SaveChangesAsync();
    }
}
