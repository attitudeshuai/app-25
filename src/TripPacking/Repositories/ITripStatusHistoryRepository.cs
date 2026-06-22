using TripPacking.Entities;

namespace TripPacking.Repositories;

public interface ITripStatusHistoryRepository
{
    Task AddAsync(TripStatusHistory history);
    Task<IEnumerable<TripStatusHistory>> GetByTripIdAsync(int tripId);
}
