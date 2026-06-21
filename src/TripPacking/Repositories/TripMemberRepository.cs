using Microsoft.EntityFrameworkCore;
using TripPacking.Data;
using TripPacking.DTOs;
using TripPacking.Entities;

namespace TripPacking.Repositories;

public class TripMemberRepository : IRepository<TripMember>, ITripMemberRepository
{
    private readonly AppDbContext _context;

    public TripMemberRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TripMember>> GetAllAsync()
    {
        return await _context.Set<TripMember>()
            .Include(tm => tm.Trip)
            .Include(tm => tm.User)
            .ToListAsync();
    }

    public async Task<TripMember?> GetByIdAsync(int id)
    {
        return await _context.Set<TripMember>()
            .Include(tm => tm.Trip)
            .Include(tm => tm.User)
            .FirstOrDefaultAsync(tm => tm.Id == id);
    }

    public async Task AddAsync(TripMember entity)
    {
        await _context.Set<TripMember>().AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(TripMember entity)
    {
        _context.Set<TripMember>().Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(TripMember entity)
    {
        _context.Set<TripMember>().Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<TripMember>> GetByTripIdAsync(int tripId)
    {
        return await _context.Set<TripMember>()
            .Include(tm => tm.Trip)
            .Include(tm => tm.User)
            .Where(tm => tm.TripId == tripId)
            .ToListAsync();
    }

    public async Task<IEnumerable<TripMember>> GetByUserIdAsync(int userId)
    {
        return await _context.Set<TripMember>()
            .Include(tm => tm.Trip)
            .Include(tm => tm.User)
            .Where(tm => tm.UserId == userId)
            .ToListAsync();
    }

    public async Task<TripMember?> GetByTripAndUserIdAsync(int tripId, int userId)
    {
        return await _context.Set<TripMember>()
            .Include(tm => tm.Trip)
            .Include(tm => tm.User)
            .FirstOrDefaultAsync(tm => tm.TripId == tripId && tm.UserId == userId);
    }

    public async Task<PagedResult<TripMember>> GetPagedAsync(int pageIndex, int pageSize, string? keyword, int? tripId, string? role, int? currentUserId = null)
    {
        var query = _context.Set<TripMember>()
            .Include(tm => tm.Trip)
                .ThenInclude(t => t.TripMembers)
            .Include(tm => tm.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(tm => tm.User.Username.Contains(keyword) || tm.User.Email.Contains(keyword));

        if (tripId.HasValue)
            query = query.Where(tm => tm.TripId == tripId.Value);

        if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<MemberRole>(role, true, out var parsedRole))
            query = query.Where(tm => tm.Role == parsedRole);

        if (currentUserId.HasValue)
            query = query.Where(tm => tm.Trip.OwnerId == currentUserId.Value || tm.Trip.TripMembers.Any(x => x.UserId == currentUserId.Value));

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(tm => tm.JoinedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<TripMember> { Items = items, Total = total };
    }
}
