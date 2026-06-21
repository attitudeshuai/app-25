using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripPacking.DTOs;
using TripPacking.Services;

namespace TripPacking.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PackingItemsController : ControllerBase
{
    private readonly IPackingItemService _packingItemService;

    public PackingItemsController(IPackingItemService packingItemService)
    {
        _packingItemService = packingItemService;
    }

    [HttpGet]
    public async Task<ApiResponse<PagedResult<PackingItemDto>>> GetPaged([FromQuery] PackingItemQueryDto query)
    {
        var userId = GetCurrentUserId();
        var result = await _packingItemService.GetPaged(query, userId);
        return ApiResponse<PagedResult<PackingItemDto>>.Success(result);
    }

    [HttpPost]
    public async Task<ApiResponse<PackingItemDto>> Create([FromBody] CreatePackingItemDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _packingItemService.Create(dto, userId);
        return ApiResponse<PackingItemDto>.Success(result);
    }

    [HttpGet("{id}")]
    public async Task<ApiResponse<PackingItemDto>> GetById(int id)
    {
        var userId = GetCurrentUserId();
        var result = await _packingItemService.GetById(id, userId);
        return ApiResponse<PackingItemDto>.Success(result);
    }

    [HttpPut("{id}")]
    public async Task<ApiResponse<PackingItemDto>> Update(int id, [FromBody] UpdatePackingItemDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _packingItemService.Update(id, dto, userId);
        return ApiResponse<PackingItemDto>.Success(result);
    }

    [HttpDelete("{id}")]
    public async Task<ApiResponse<bool>> Delete(int id)
    {
        var userId = GetCurrentUserId();
        await _packingItemService.Delete(id, userId);
        return ApiResponse<bool>.Success(true);
    }

    private int GetCurrentUserId()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdStr, out var id) ? id : 0;
    }
}
