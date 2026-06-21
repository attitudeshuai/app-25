using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripPacking.DTOs;
using TripPacking.Services;

namespace TripPacking.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<ApiResponse<PagedResult<NotificationDto>>> GetPaged([FromQuery] NotificationQueryDto query)
    {
        var userId = GetCurrentUserId();
        var result = await _notificationService.GetPagedAsync(query, userId);
        return ApiResponse<PagedResult<NotificationDto>>.Success(result);
    }

    [HttpGet("{id}")]
    public async Task<ApiResponse<NotificationDto>> GetById(int id)
    {
        var userId = GetCurrentUserId();
        var result = await _notificationService.GetByIdAsync(id, userId);
        return ApiResponse<NotificationDto>.Success(result);
    }

    [HttpGet("unread-count")]
    public async Task<ApiResponse<int>> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        var count = await _notificationService.GetUnreadCountAsync(userId);
        return ApiResponse<int>.Success(count);
    }

    [HttpPost("mark-read")]
    public async Task<ApiResponse<bool>> MarkAsRead([FromBody] MarkNotificationsReadDto dto)
    {
        var userId = GetCurrentUserId();
        if (dto.NotificationIds == null || dto.NotificationIds.Length == 0)
            await _notificationService.MarkAllAsReadAsync(userId);
        else
            await _notificationService.MarkAsReadAsync(dto.NotificationIds, userId);
        return ApiResponse<bool>.Success(true);
    }

    [HttpDelete("{id}")]
    public async Task<ApiResponse<bool>> Delete(int id)
    {
        var userId = GetCurrentUserId();
        await _notificationService.DeleteAsync(id, userId);
        return ApiResponse<bool>.Success(true);
    }

    private int GetCurrentUserId()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdStr, out var id) ? id : 0;
    }
}
