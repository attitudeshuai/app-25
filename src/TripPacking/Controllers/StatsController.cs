using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripPacking.DTOs;
using TripPacking.Services;

namespace TripPacking.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StatsController : ControllerBase
{
    private readonly IStatsService _statsService;

    public StatsController(IStatsService statsService)
    {
        _statsService = statsService;
    }

    [HttpGet("overview")]
    public async Task<ApiResponse<StatsOverviewDto>> GetOverview()
    {
        var result = await _statsService.GetOverview();
        return ApiResponse<StatsOverviewDto>.Success(result);
    }

    [HttpGet("trend")]
    public async Task<ApiResponse<IEnumerable<StatsTrendDto>>> GetTrend([FromQuery] DateTime start, [FromQuery] DateTime end)
    {
        var result = await _statsService.GetTrend(start, end);
        return ApiResponse<IEnumerable<StatsTrendDto>>.Success(result);
    }
}
