using TripPacking.DTOs;

namespace TripPacking.Services;

public interface IStatsService
{
    Task<StatsOverviewDto> GetOverview();
    Task<IEnumerable<StatsTrendDto>> GetTrend(DateTime start, DateTime end);
}
