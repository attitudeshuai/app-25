using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripPacking.DTOs;
using TripPacking.Services;

namespace TripPacking.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PackingCategoriesController : ControllerBase
{
    private readonly IPackingCategoryService _packingCategoryService;

    public PackingCategoriesController(IPackingCategoryService packingCategoryService)
    {
        _packingCategoryService = packingCategoryService;
    }

    [HttpGet]
    public async Task<ApiResponse<PagedResult<PackingCategoryDto>>> GetPaged([FromQuery] PackingCategoryQueryDto query)
    {
        var userId = GetCurrentUserId();
        var result = await _packingCategoryService.GetPaged(query, userId);
        return ApiResponse<PagedResult<PackingCategoryDto>>.Success(result);
    }

    [HttpPost]
    public async Task<ApiResponse<PackingCategoryDto>> Create([FromBody] CreatePackingCategoryDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _packingCategoryService.Create(dto, userId);
        return ApiResponse<PackingCategoryDto>.Success(result);
    }

    [HttpGet("{id}")]
    public async Task<ApiResponse<PackingCategoryDto>> GetById(int id)
    {
        var userId = GetCurrentUserId();
        var result = await _packingCategoryService.GetById(id, userId);
        return ApiResponse<PackingCategoryDto>.Success(result);
    }

    [HttpPut("{id}")]
    public async Task<ApiResponse<PackingCategoryDto>> Update(int id, [FromBody] UpdatePackingCategoryDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _packingCategoryService.Update(id, dto, userId);
        return ApiResponse<PackingCategoryDto>.Success(result);
    }

    [HttpDelete("{id}")]
    public async Task<ApiResponse<bool>> Delete(int id)
    {
        var userId = GetCurrentUserId();
        await _packingCategoryService.Delete(id, userId);
        return ApiResponse<bool>.Success(true);
    }

    private int GetCurrentUserId()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdStr, out var id) ? id : 0;
    }
}
