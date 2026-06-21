using Microsoft.EntityFrameworkCore;
using TripPacking.Data;
using TripPacking.DTOs;
using TripPacking.Entities;

namespace TripPacking.Repositories;

public class NotificationRepository : IRepository<Notification>, INotificationRepository
{
    private readonly AppDbContext _context;

    public NotificationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Notification>> GetAllAsync()
    {
        return await _context.Set<Notification>()
            .Include(n => n.User)
            .ToListAsync();
    }

    public async Task<Notification?> GetByIdAsync(int id)
    {
        return await _context.Set<Notification>()
            .Include(n => n.User)
            .FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task AddAsync(Notification entity)
    {
        await _context.Set<Notification>().AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Notification entity)
    {
        _context.Set<Notification>().Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Notification entity)
    {
        _context.Set<Notification>().Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Notification>> GetByUserIdAsync(int userId)
    {
        return await _context.Set<Notification>()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<PagedResult<Notification>> GetPagedAsync(int pageIndex, int pageSize, int userId, bool? isRead, NotificationType? type)
    {
        var query = _context.Set<Notification>()
            .AsQueryable();

        query = query.Where(n => n.UserId == userId);

        if (isRead.HasValue)
            query = query.Where(n => n.IsRead == isRead.Value);

        if (type.HasValue)
            query = query.Where(n => n.Type == type.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Notification> { Items = items, Total = total };
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _context.Set<Notification>()
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task MarkAsReadAsync(int[] notificationIds, int userId)
    {
        var notifications = await _context.Set<Notification>()
            .Where(n => notificationIds.Contains(n.Id) && n.UserId == userId && !n.IsRead)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
        }

        await _context.SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        var notifications = await _context.Set<Notification>()
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
        }

        await _context.SaveChangesAsync();
    }
}
