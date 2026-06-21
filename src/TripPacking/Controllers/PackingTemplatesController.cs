using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripPacking.DTOs;
using TripPacking.Services;

namespace TripPacking.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PackingTemplatesController : ControllerBase
{
    private readonly IPackingTemplateService _packingTemplateService;

    public PackingTemplatesController(IPackingTemplateService packingTemplateService)
    {
        _packingTemplateService = packingTemplateService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ApiResponse<PagedResult<PackingTemplateDto>>> GetPaged([FromQuery] PackingTemplateQueryDto query)
    {
        var result = await _packingTemplateService.GetPaged(query);
        return ApiResponse<PagedResult<PackingTemplateDto>>.Success(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<ApiResponse<PackingTemplateDto>> Create([FromBody] CreatePackingTemplateDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _packingTemplateService.Create(dto, userId);
        return ApiResponse<PackingTemplateDto>.Success(result);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ApiResponse<PackingTemplateDto>> GetById(int id)
    {
        var result = await _packingTemplateService.GetById(id);
        return ApiResponse<PackingTemplateDto>.Success(result);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ApiResponse<PackingTemplateDto>> Update(int id, [FromBody] UpdatePackingTemplateDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _packingTemplateService.Update(id, dto, userId);
        return ApiResponse<PackingTemplateDto>.Success(result);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ApiResponse<bool>> Delete(int id)
    {
        var userId = GetCurrentUserId();
        await _packingTemplateService.Delete(id, userId);
        return ApiResponse<bool>.Success(true);
    }

    private int GetCurrentUserId()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdStr, out var id) ? id : 0;
    }
}
