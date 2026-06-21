using TripPacking.DTOs;
using TripPacking.Entities;

namespace TripPacking.Repositories;

public interface INotificationRepository : IRepository<Notification>
{
    Task<IEnumerable<Notification>> GetByUserIdAsync(int userId);
    Task<PagedResult<Notification>> GetPagedAsync(int pageIndex, int pageSize, int userId, bool? isRead, NotificationType? type);
    Task<int> GetUnreadCountAsync(int userId);
    Task MarkAsReadAsync(int[] notificationIds, int userId);
    Task MarkAllAsReadAsync(int userId);
}
