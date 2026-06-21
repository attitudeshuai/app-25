using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripPacking.DTOs;
using TripPacking.Services;

namespace TripPacking.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TripsController : ControllerBase
{
    private readonly ITripService _tripService;

    public TripsController(ITripService tripService)
    {
        _tripService = tripService;
    }

    [HttpGet]
    public async Task<ApiResponse<object>> GetPaged([FromQuery] TripQueryDto query)
    {
        var userId = GetCurrentUserId();
        var result = await _tripService.GetPaged(query, userId);
        return ApiResponse<object>.Success(result);
    }

    [HttpPost]
    public async Task<ApiResponse<TripDto>> Create([FromBody] CreateTripDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _tripService.Create(dto, userId);
        return ApiResponse<TripDto>.Success(result);
    }

    [HttpGet("{id}")]
    public async Task<ApiResponse<TripDto>> GetById(int id)
    {
        var userId = GetCurrentUserId();
        var result = await _tripService.GetById(id, userId);
        return ApiResponse<TripDto>.Success(result);
    }

    [HttpPut("{id}")]
    public async Task<ApiResponse<TripDto>> Update(int id, [FromBody] UpdateTripDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _tripService.Update(id, dto, userId);
        return ApiResponse<TripDto>.Success(result);
    }

    [HttpDelete("{id}")]
    public async Task<ApiResponse<bool>> Delete(int id)
    {
        var userId = GetCurrentUserId();
        await _tripService.Delete(id, userId);
        return ApiResponse<bool>.Success(true);
    }

    [HttpPatch("{id}/status")]
    public async Task<ApiResponse<TripDto>> UpdateStatus(int id, [FromBody] UpdateTripStatusDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _tripService.UpdateStatus(id, dto, userId);
        return ApiResponse<TripDto>.Success(result);
    }

    [HttpGet("mine")]
    public async Task<ApiResponse<PagedResult<TripDto>>> GetMine([FromQuery] TripQueryDto query)
    {
        var userId = GetCurrentUserId();
        var result = await _tripService.GetMine(userId, query);
        return ApiResponse<PagedResult<TripDto>>.Success(result);
    }

    private int GetCurrentUserId()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdStr, out var id) ? id : 0;
    }
}
