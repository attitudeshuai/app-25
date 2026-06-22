using TripPacking.Entities;
using TripPacking.Repositories;

namespace TripPacking.Services;

public class PackingDefaultsInitializerService : IPackingDefaultsInitializerService
{
    private readonly IPackingCategoryRepository _packingCategoryRepository;
    private readonly IPackingItemRepository _packingItemRepository;
    private readonly ITripRepository _tripRepository;

    private static readonly List<(string Name, int SortOrder, List<string> Items)> DefaultCategories = new()
    {
        ("衣物", 1, new List<string>
        {
            "T恤/上衣",
            "裤子/裙子",
            "内衣内裤",
            "袜子",
            "外套/风衣",
            "睡衣",
            "运动鞋",
            "拖鞋"
        }),
        ("洗漱", 2, new List<string>
        {
            "牙刷",
            "牙膏",
            "毛巾",
            "洗发水",
            "沐浴露",
            "洗面奶",
            "护肤品",
            "剃须刀"
        }),
        ("证件", 3, new List<string>
        {
            "身份证",
            "护照",
            "驾驶证",
            "车票/机票",
            "酒店预订确认",
            "信用卡/银行卡",
            "现金"
        }),
        ("电子", 4, new List<string>
        {
            "手机",
            "手机充电器",
            "充电宝",
            "耳机",
            "相机/摄像机",
            "转换插头",
            "数据线"
        }),
        ("药品", 5, new List<string>
        {
            "感冒药",
            "肠胃药",
            "创可贴",
            "退烧药",
            "晕车药",
            "消炎药",
            "维生素"
        })
    };

    public PackingDefaultsInitializerService(
        IPackingCategoryRepository packingCategoryRepository,
        IPackingItemRepository packingItemRepository,
        ITripRepository tripRepository)
    {
        _packingCategoryRepository = packingCategoryRepository;
        _packingItemRepository = packingItemRepository;
        _tripRepository = tripRepository;
    }

    public async Task InitializeDefaultCategoriesAsync(int tripId)
    {
        var trip = await _tripRepository.GetByIdAsync(tripId);
        if (trip == null)
            return;

        var deletedCategories = trip.GetDeletedDefaultCategories();
        var existingCategories = await _packingCategoryRepository.GetByTripIdAsync(tripId);
        var existingCategoryNames = existingCategories.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var (name, sortOrder, items) in DefaultCategories)
        {
            if (deletedCategories.Contains(name, StringComparer.OrdinalIgnoreCase))
                continue;

            if (existingCategoryNames.Contains(name))
                continue;

            var category = new PackingCategory
            {
                TripId = tripId,
                Name = name,
                SortOrder = sortOrder,
                CreatedAt = DateTime.UtcNow
            };

            await _packingCategoryRepository.AddAsync(category);

            foreach (var itemName in items)
            {
                var item = new PackingItem
                {
                    TripId = tripId,
                    CategoryId = category.Id,
                    Name = itemName,
                    Quantity = 1,
                    IsPacked = false,
                    IsShared = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _packingItemRepository.AddAsync(item);
            }
        }
    }

    public static bool IsDefaultCategory(string categoryName)
    {
        return DefaultCategories.Any(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
    }
}
