namespace TripPacking.Services;

public interface IPackingDefaultsInitializerService
{
    Task InitializeDefaultCategoriesAsync(int tripId);
}
