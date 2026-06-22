using AutoMapper;
using TripPacking.DTOs;
using TripPacking.Entities;
using TripPacking.Repositories;

namespace TripPacking.Services;

public class PackingCategoryService : IPackingCategoryService
{
    private const int MinSortOrder = 0;
    private const int MaxSortOrder = 10000;

    private readonly IPackingCategoryRepository _packingCategoryRepository;
    private readonly ITripRepository _tripRepository;
    private readonly ITripMemberRepository _tripMemberRepository;
    private readonly IMapper _mapper;

    public PackingCategoryService(IPackingCategoryRepository packingCategoryRepository, ITripRepository tripRepository, ITripMemberRepository tripMemberRepository, IMapper mapper)
    {
        _packingCategoryRepository = packingCategoryRepository;
        _tripRepository = tripRepository;
        _tripMemberRepository = tripMemberRepository;
        _mapper = mapper;
    }

    private async Task<bool> HasTripAccess(int tripId, int userId)
    {
        var trip = await _tripRepository.GetByIdAsync(tripId);
        if (trip == null)
            return false;

        if (trip.OwnerId == userId)
            return true;

        var members = await _tripMemberRepository.GetByTripIdAsync(tripId);
        return members.Any(m => m.UserId == userId);
    }

    private async Task<bool> IsTripOwner(int tripId, int userId)
    {
        var trip = await _tripRepository.GetByIdAsync(tripId);
        return trip != null && trip.OwnerId == userId;
    }

    private static void ValidateSortOrderRange(int sortOrder, string paramName = "SortOrder")
    {
        if (sortOrder < MinSortOrder || sortOrder > MaxSortOrder)
            throw new ArgumentException($"排序序号必须在{MinSortOrder}到{MaxSortOrder}之间", paramName);
    }

    private static void ValidateSortOrdersRange(IEnumerable<(int CategoryId, int SortOrder)> items)
    {
        var errors = new List<string>();
        foreach (var item in items)
        {
            if (item.SortOrder < MinSortOrder || item.SortOrder > MaxSortOrder)
                errors.Add($"分类ID {item.CategoryId} 的排序序号必须在{MinSortOrder}到{MaxSortOrder}之间");
        }
        if (errors.Any())
            throw new ArgumentException(string.Join("; ", errors));
    }

    private static void ValidateNoDuplicateSortOrders(IEnumerable<(int CategoryId, int SortOrder)> items)
    {
        var sortOrderGroups = items.GroupBy(x => x.SortOrder).Where(g => g.Count() > 1).ToList();
        if (sortOrderGroups.Any())
        {
            var errors = sortOrderGroups.Select(g =>
            {
                var categoryIds = string.Join(", ", g.Select(x => x.CategoryId));
                return $"排序序号 {g.Key} 被多个分类使用（分类ID: {categoryIds}）";
            });
            throw new ArgumentException($"存在重复的排序序号: {string.Join("; ", errors)}");
        }
    }

    private async Task ValidateSortOrderUnique(int tripId, int sortOrder, int? excludeCategoryId = null)
    {
        if (await _packingCategoryRepository.IsSortOrderDuplicateAsync(tripId, sortOrder, excludeCategoryId))
            throw new InvalidOperationException($"排序序号 {sortOrder} 已被该旅行下的其他分类使用，请选择其他序号");
    }

    private async Task ValidateAllSortOrdersUnique(int tripId, IEnumerable<(int CategoryId, int SortOrder)> items)
    {
        var existingCategories = await _packingCategoryRepository.GetByTripIdAsync(tripId);
        var existingDict = existingCategories.ToDictionary(c => c.Id, c => c.SortOrder);
        var itemDict = items.ToDictionary(x => x.CategoryId, x => x.SortOrder);

        var allSortOrdersWithCategory = new List<(int CategoryId, int SortOrder)>();
        foreach (var cat in existingCategories)
        {
            var sortOrder = itemDict.TryGetValue(cat.Id, out var newSort) ? newSort : cat.SortOrder;
            allSortOrdersWithCategory.Add((cat.Id, sortOrder));
        }

        ValidateNoDuplicateSortOrders(allSortOrdersWithCategory);
    }

    public async Task<PagedResult<PackingCategoryDto>> GetPaged(PackingCategoryQueryDto query, int currentUserId)
    {
        if (query.TripId.HasValue && !await HasTripAccess(query.TripId.Value, currentUserId))
            throw new UnauthorizedAccessException("No access to this trip");

        var pagedResult = await _packingCategoryRepository.GetPagedAsync(query.PageIndex, query.PageSize, query.Keyword, query.TripId);

        var items = pagedResult.Items;
        var total = pagedResult.Total;

        if (!query.TripId.HasValue)
        {
            var userMemberships = await _tripMemberRepository.GetByUserIdAsync(currentUserId);
            var ownedTrips = await _tripRepository.GetByOwnerIdAsync(currentUserId);
            var accessibleTripIds = userMemberships.Select(m => m.TripId).Concat(ownedTrips.Select(t => t.Id)).ToHashSet();
            items = items.Where(c => accessibleTripIds.Contains(c.TripId)).ToList();
            total = items.Count();
        }

        return new PagedResult<PackingCategoryDto> { Items = _mapper.Map<IEnumerable<PackingCategoryDto>>(items), Total = total };
    }

    public async Task<PackingCategoryDto> GetById(int id, int currentUserId)
    {
        var category = await _packingCategoryRepository.GetByIdAsync(id);
        if (category == null)
            throw new KeyNotFoundException("Packing category not found");

        if (!await HasTripAccess(category.TripId, currentUserId))
            throw new UnauthorizedAccessException("No access to this trip");

        return _mapper.Map<PackingCategoryDto>(category);
    }

    public async Task<PackingCategoryDto> Create(CreatePackingCategoryDto dto, int currentUserId)
    {
        if (!await IsTripOwner(dto.TripId, currentUserId))
            throw new UnauthorizedAccessException("Only trip owner can create packing categories");

        ValidateSortOrderRange(dto.SortOrder);
        await ValidateSortOrderUnique(dto.TripId, dto.SortOrder);

        var category = new PackingCategory
        {
            TripId = dto.TripId,
            Name = dto.Name,
            SortOrder = dto.SortOrder,
            CreatedAt = DateTime.UtcNow
        };

        await _packingCategoryRepository.AddAsync(category);
        return _mapper.Map<PackingCategoryDto>(category);
    }

    public async Task<PackingCategoryDto> Update(int id, UpdatePackingCategoryDto dto, int currentUserId)
    {
        var category = await _packingCategoryRepository.GetByIdAsync(id);
        if (category == null)
            throw new KeyNotFoundException("Packing category not found");

        if (!await IsTripOwner(category.TripId, currentUserId))
            throw new UnauthorizedAccessException("Only trip owner can update packing categories");

        if (!string.IsNullOrWhiteSpace(dto.Name))
            category.Name = dto.Name;

        if (dto.SortOrder.HasValue)
        {
            ValidateSortOrderRange(dto.SortOrder.Value);
            await ValidateSortOrderUnique(category.TripId, dto.SortOrder.Value, category.Id);
            category.SortOrder = dto.SortOrder.Value;
        }

        await _packingCategoryRepository.UpdateAsync(category);
        return _mapper.Map<PackingCategoryDto>(category);
    }

    public async Task Delete(int id, int currentUserId)
    {
        var category = await _packingCategoryRepository.GetByIdAsync(id);
        if (category == null)
            throw new KeyNotFoundException("Packing category not found");

        if (!await IsTripOwner(category.TripId, currentUserId))
            throw new UnauthorizedAccessException("Only trip owner can delete packing categories");

        if (PackingDefaultsInitializerService.IsDefaultCategory(category.Name))
        {
            var trip = await _tripRepository.GetByIdAsync(category.TripId);
            if (trip != null)
            {
                trip.AddDeletedDefaultCategory(category.Name);
                await _tripRepository.UpdateAsync(trip);
            }
        }

        await _packingCategoryRepository.DeleteAsync(category);
    }

    public async Task<IEnumerable<PackingCategoryDto>> UpdateSortOrders(UpdateCategorySortOrderDto dto, int currentUserId)
    {
        if (!await IsTripOwner(dto.TripId, currentUserId))
            throw new UnauthorizedAccessException("Only trip owner can update packing category sort orders");

        if (dto.Items == null || !dto.Items.Any())
            throw new ArgumentException("排序更新列表不能为空");

        var items = dto.Items.Select(x => (x.CategoryId, x.SortOrder)).ToList();

        ValidateSortOrdersRange(items);
        ValidateNoDuplicateSortOrders(items);
        await ValidateAllSortOrdersUnique(dto.TripId, items);

        var categoryIds = items.Select(x => x.CategoryId).Distinct().ToList();
        var existingCategories = await _packingCategoryRepository.GetByIdsAsync(categoryIds);
        var existingCategoryIds = existingCategories.Select(c => c.Id).ToHashSet();

        var invalidIds = categoryIds.Where(id => !existingCategoryIds.Contains(id)).ToList();
        if (invalidIds.Any())
            throw new KeyNotFoundException($"以下分类ID不存在: {string.Join(", ", invalidIds)}");

        var invalidTripIds = existingCategories.Where(c => c.TripId != dto.TripId).Select(c => c.Id).ToList();
        if (invalidTripIds.Any())
            throw new ArgumentException($"以下分类不属于指定旅行（TripId: {dto.TripId}）: {string.Join(", ", invalidTripIds)}");

        var sortOrderDict = items.ToDictionary(x => x.CategoryId, x => x.SortOrder);
        foreach (var category in existingCategories)
        {
            category.SortOrder = sortOrderDict[category.Id];
        }

        await _packingCategoryRepository.UpdateRangeAsync(existingCategories);

        var allCategories = await _packingCategoryRepository.GetByTripIdAsync(dto.TripId);
        return _mapper.Map<IEnumerable<PackingCategoryDto>>(allCategories.OrderBy(c => c.SortOrder));
    }
}
