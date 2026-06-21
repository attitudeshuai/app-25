using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripPacking.DTOs;
using TripPacking.Services;

namespace TripPacking.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvitationsController : ControllerBase
{
    private readonly IInvitationService _invitationService;

    public InvitationsController(IInvitationService invitationService)
    {
        _invitationService = invitationService;
    }

    [HttpPost]
    public async Task<ApiResponse<InvitationDto>> Create([FromBody] CreateInvitationDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _invitationService.CreateAsync(dto, userId);
        return ApiResponse<InvitationDto>.Success(result);
    }

    [HttpGet("{id}")]
    public async Task<ApiResponse<InvitationDto>> GetById(int id)
    {
        var userId = GetCurrentUserId();
        var result = await _invitationService.GetByIdAsync(id, userId);
        return ApiResponse<InvitationDto>.Success(result);
    }

    [HttpGet]
    public async Task<ApiResponse<PagedResult<InvitationDto>>> GetPaged([FromQuery] InvitationQueryDto query)
    {
        var userId = GetCurrentUserId();
        var result = await _invitationService.GetPagedAsync(query, userId);
        return ApiResponse<PagedResult<InvitationDto>>.Success(result);
    }

    [HttpPost("{id}/respond")]
    public async Task<ApiResponse<InvitationDto>> Respond(int id, [FromBody] RespondInvitationDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _invitationService.RespondAsync(id, dto, userId);
        return ApiResponse<InvitationDto>.Success(result);
    }

    [HttpPost("{id}/cancel")]
    public async Task<ApiResponse<bool>> Cancel(int id)
    {
        var userId = GetCurrentUserId();
        await _invitationService.CancelAsync(id, userId);
        return ApiResponse<bool>.Success(true);
    }

    private int GetCurrentUserId()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdStr, out var id) ? id : 0;
    }
}
