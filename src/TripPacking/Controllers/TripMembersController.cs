using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripPacking.DTOs;
using TripPacking.Services;

namespace TripPacking.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TripMembersController : ControllerBase
{
    private readonly ITripMemberService _tripMemberService;

    public TripMembersController(ITripMemberService tripMemberService)
    {
        _tripMemberService = tripMemberService;
    }

    [HttpGet]
    public async Task<ApiResponse<object>> GetPaged([FromQuery] TripMemberQueryDto query)
    {
        var userId = GetCurrentUserId();
        var result = await _tripMemberService.GetPaged(query, userId);
        return ApiResponse<object>.Success(result);
    }

    [HttpPost]
    public async Task<ApiResponse<TripMemberDto>> Create([FromBody] CreateTripMemberDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _tripMemberService.Create(dto, userId);
        return ApiResponse<TripMemberDto>.Success(result);
    }

    [HttpGet("{id}")]
    public async Task<ApiResponse<TripMemberDto>> GetById(int id)
    {
        var userId = GetCurrentUserId();
        var result = await _tripMemberService.GetById(id, userId);
        return ApiResponse<TripMemberDto>.Success(result);
    }

    [HttpPut("{id}")]
    public async Task<ApiResponse<TripMemberDto>> Update(int id, [FromBody] UpdateTripMemberDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _tripMemberService.Update(id, dto, userId);
        return ApiResponse<TripMemberDto>.Success(result);
    }

    [HttpDelete("{id}")]
    public async Task<ApiResponse<bool>> Delete(int id)
    {
        var userId = GetCurrentUserId();
        await _tripMemberService.Delete(id, userId);
        return ApiResponse<bool>.Success(true);
    }

    [HttpGet("mine")]
    public async Task<ApiResponse<PagedResult<TripMemberDto>>> GetMine([FromQuery] TripMemberQueryDto query)
    {
        var userId = GetCurrentUserId();
        var result = await _tripMemberService.GetMine(userId, query);
        return ApiResponse<PagedResult<TripMemberDto>>.Success(result);
    }

    private int GetCurrentUserId()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdStr, out var id) ? id : 0;
    }
}
