using TripPacking.DTOs;
using TripPacking.Repositories;

namespace TripPacking.Services;

public class StatsService : IStatsService
{
    private readonly IUserRepository _userRepository;
    private readonly ITripRepository _tripRepository;
    private readonly IPackingItemRepository _packingItemRepository;

    public StatsService(IUserRepository userRepository, ITripRepository tripRepository, IPackingItemRepository packingItemRepository)
    {
        _userRepository = userRepository;
        _tripRepository = tripRepository;
        _packingItemRepository = packingItemRepository;
    }

    public async Task<StatsOverviewDto> GetOverview()
    {
        var users = await _userRepository.GetAllAsync();
        var trips = await _tripRepository.GetAllAsync();
        var items = await _packingItemRepository.GetAllAsync();

        var itemsList = items.ToList();
        var packedItems = itemsList.Count(i => i.IsPacked);
        var unpackedItems = itemsList.Count(i => !i.IsPacked);
        var totalItems = itemsList.Count;

        var progress = totalItems > 0 ? (double)packedItems / totalItems * 100 : 0;

        return new StatsOverviewDto
        {
            TotalUsers = users.Count(),
            TotalTrips = trips.Count(),
            TotalItems = totalItems,
            PackedItems = packedItems,
            UnpackedItems = unpackedItems,
            PackingProgress = Math.Round(progress, 2)
        };
    }

    public async Task<IEnumerable<StatsTrendDto>> GetTrend(DateTime start, DateTime end)
    {
        var trips = await _tripRepository.GetAllAsync();
        var items = await _packingItemRepository.GetAllAsync();

        var tripsInRange = trips.Where(t => t.CreatedAt.Date >= start.Date && t.CreatedAt.Date <= end.Date).ToList();
        var itemsInRange = items.Where(i => i.CreatedAt.Date >= start.Date && i.CreatedAt.Date <= end.Date).ToList();

        var result = new List<StatsTrendDto>();
        for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
        {
            var dateStr = date.ToString("yyyy-MM-dd");
            result.Add(new StatsTrendDto
            {
                Date = dateStr,
                Trips = tripsInRange.Count(t => t.CreatedAt.Date == date),
                Items = itemsInRange.Count(i => i.CreatedAt.Date == date)
            });
        }

        return result;
    }
}
