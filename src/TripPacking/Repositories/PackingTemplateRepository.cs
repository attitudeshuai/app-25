using Microsoft.EntityFrameworkCore;
using TripPacking.Data;
using TripPacking.DTOs;
using TripPacking.Entities;

namespace TripPacking.Repositories;

public class PackingTemplateRepository : IRepository<PackingTemplate>, IPackingTemplateRepository
{
    private readonly AppDbContext _context;

    public PackingTemplateRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PackingTemplate>> GetAllAsync()
    {
        return await _context.Set<PackingTemplate>()
            .Include(pt => pt.Creator)
            .ToListAsync();
    }

    public async Task<PackingTemplate?> GetByIdAsync(int id)
    {
        return await _context.Set<PackingTemplate>()
            .Include(pt => pt.Creator)
            .FirstOrDefaultAsync(pt => pt.Id == id);
    }

    public async Task AddAsync(PackingTemplate entity)
    {
        await _context.Set<PackingTemplate>().AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(PackingTemplate entity)
    {
        _context.Set<PackingTemplate>().Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(PackingTemplate entity)
    {
        _context.Set<PackingTemplate>().Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<PagedResult<PackingTemplate>> GetPagedAsync(int pageIndex, int pageSize, string? keyword, string? category)
    {
        var query = _context.Set<PackingTemplate>()
            .Include(pt => pt.Creator)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(pt => pt.Name.Contains(keyword));

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(pt => pt.Category == category);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(pt => pt.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<PackingTemplate> { Items = items, Total = total };
    }
}
