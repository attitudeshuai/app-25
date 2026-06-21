using System.Text.Json;
using AutoMapper;
using TripPacking.DTOs;
using TripPacking.Entities;
using TripPacking.Repositories;

namespace TripPacking.Services;

public class PackingTemplateService : IPackingTemplateService
{
    private readonly IPackingTemplateRepository _packingTemplateRepository;
    private readonly IPackingCategoryRepository _packingCategoryRepository;
    private readonly IPackingItemRepository _packingItemRepository;
    private readonly ITripRepository _tripRepository;
    private readonly ITripMemberRepository _tripMemberRepository;
    private readonly IMapper _mapper;

    public PackingTemplateService(
        IPackingTemplateRepository packingTemplateRepository,
        IPackingCategoryRepository packingCategoryRepository,
        IPackingItemRepository packingItemRepository,
        ITripRepository tripRepository,
        ITripMemberRepository tripMemberRepository,
        IMapper mapper)
    {
        _packingTemplateRepository = packingTemplateRepository;
        _packingCategoryRepository = packingCategoryRepository;
        _packingItemRepository = packingItemRepository;
        _tripRepository = tripRepository;
        _tripMemberRepository = tripMemberRepository;
        _mapper = mapper;
    }

    public async Task<PagedResult<PackingTemplateDto>> GetPaged(PackingTemplateQueryDto query)
    {
        var pagedResult = await _packingTemplateRepository.GetPagedAsync(query.PageIndex, query.PageSize, query.Keyword, query.Category);
        return new PagedResult<PackingTemplateDto> { Items = _mapper.Map<IEnumerable<PackingTemplateDto>>(pagedResult.Items), Total = pagedResult.Total };
    }

    public async Task<PackingTemplateDto> GetById(int id)
    {
        var template = await _packingTemplateRepository.GetByIdAsync(id);
        if (template == null)
            throw new KeyNotFoundException("Packing template not found");

        return _mapper.Map<PackingTemplateDto>(template);
    }

    public async Task<ParsedTemplateDto> GetParsedById(int id)
    {
        var template = await _packingTemplateRepository.GetByIdAsync(id);
        if (template == null)
            throw new KeyNotFoundException("Packing template not found");

        var categories = ParseItemsJson(template.ItemsJson);
        var totalItems = categories.Sum(c => c.Items.Count);

        return new ParsedTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Category = template.Category,
            Categories = categories,
            TotalItems = totalItems,
            CreatedBy = template.CreatedBy,
            CreatedAt = template.CreatedAt
        };
    }

    public async Task<PackingTemplateDto> Create(CreatePackingTemplateDto dto, int currentUserId)
    {
        var template = new PackingTemplate
        {
            Name = dto.Name,
            Category = dto.Category ?? string.Empty,
            ItemsJson = dto.ItemsJson,
            CreatedBy = currentUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _packingTemplateRepository.AddAsync(template);
        return _mapper.Map<PackingTemplateDto>(template);
    }

    public async Task<PackingTemplateDto> Update(int id, UpdatePackingTemplateDto dto, int currentUserId)
    {
        var template = await _packingTemplateRepository.GetByIdAsync(id);
        if (template == null)
            throw new KeyNotFoundException("Packing template not found");

        if (template.CreatedBy != currentUserId)
            throw new UnauthorizedAccessException("Only template creator can update this template");

        if (!string.IsNullOrWhiteSpace(dto.Name))
            template.Name = dto.Name;

        if (dto.Category != null)
            template.Category = dto.Category;

        if (!string.IsNullOrWhiteSpace(dto.ItemsJson))
            template.ItemsJson = dto.ItemsJson;

        await _packingTemplateRepository.UpdateAsync(template);
        return _mapper.Map<PackingTemplateDto>(template);
    }

    public async Task Delete(int id, int currentUserId)
    {
        var template = await _packingTemplateRepository.GetByIdAsync(id);
        if (template == null)
            throw new KeyNotFoundException("Packing template not found");

        if (template.CreatedBy != currentUserId)
            throw new UnauthorizedAccessException("Only template creator can delete this template");

        await _packingTemplateRepository.DeleteAsync(template);
    }

    public async Task<ApplyTemplateResultDto> ApplyToTrip(ApplyTemplateRequestDto request, int currentUserId)
    {
        var template = await _packingTemplateRepository.GetByIdAsync(request.TemplateId);
        if (template == null)
            throw new KeyNotFoundException("Packing template not found");

        var trip = await _tripRepository.GetByIdAsync(request.TripId);
        if (trip == null)
            throw new KeyNotFoundException("Trip not found");

        if (!await HasTripAccess(request.TripId, currentUserId))
            throw new UnauthorizedAccessException("No access to this trip");

        var result = new ApplyTemplateResultDto
        {
            TemplateId = template.Id,
            TemplateName = template.Name,
            TripId = request.TripId
        };

        List<TemplateCategoryDto> templateCategories;
        try
        {
            templateCategories = ParseItemsJson(template.ItemsJson);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse template items: {ex.Message}");
            result.PartialSuccess = false;
            return result;
        }

        var existingCategories = (await _packingCategoryRepository.GetByTripIdAsync(request.TripId)).ToList();
        var existingItems = (await _packingItemRepository.GetByTripIdAsync(request.TripId)).ToList();
        var nextSortOrder = existingCategories.Any() ? existingCategories.Max(c => c.SortOrder) + 1 : 1;

        foreach (var templateCategory in templateCategories)
        {
            var categoryResult = new AppliedCategoryResultDto
            {
                CategoryName = templateCategory.Name
            };

            PackingCategory category;
            var existingCategory = existingCategories.FirstOrDefault(c =>
                c.Name.Equals(templateCategory.Name, StringComparison.OrdinalIgnoreCase));

            if (existingCategory != null)
            {
                category = existingCategory;
                categoryResult.IsNew = false;
                categoryResult.CategoryId = existingCategory.Id;

                if (request.ConflictStrategy == ConflictResolutionStrategy.Skip)
                {
                    result.CategoriesSkipped++;
                    categoryResult.Items.AddRange(templateCategory.Items.Select(i => new AppliedItemResultDto
                    {
                        ItemName = i.Name,
                        IsSkipped = true,
                        Message = "Category already exists, skipped"
                    }));
                    result.Details.Add(categoryResult);
                    result.ItemsSkipped += templateCategory.Items.Count;
                    continue;
                }
            }
            else
            {
                try
                {
                    category = new PackingCategory
                    {
                        TripId = request.TripId,
                        Name = templateCategory.Name,
                        SortOrder = templateCategory.SortOrder > 0 ? templateCategory.SortOrder : nextSortOrder++,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _packingCategoryRepository.AddAsync(category);
                    existingCategories.Add(category);
                    categoryResult.IsNew = true;
                    categoryResult.CategoryId = category.Id;
                    result.CategoriesCreated++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Failed to create category '{templateCategory.Name}': {ex.Message}");
                    categoryResult.Items.AddRange(templateCategory.Items.Select(i => new AppliedItemResultDto
                    {
                        ItemName = i.Name,
                        IsSkipped = true,
                        Message = $"Category creation failed: {ex.Message}"
                    }));
                    result.ItemsFailed += templateCategory.Items.Count;
                    result.Details.Add(categoryResult);
                    continue;
                }
            }

            var categoryItems = existingItems.Where(i => i.CategoryId == category.Id).ToList();

            foreach (var templateItem in templateCategory.Items)
            {
                var itemResult = new AppliedItemResultDto
                {
                    ItemName = templateItem.Name
                };

                try
                {
                    var existingItem = categoryItems.FirstOrDefault(i =>
                        i.Name.Equals(templateItem.Name, StringComparison.OrdinalIgnoreCase));

                    if (existingItem != null)
                    {
                        switch (request.ConflictStrategy)
                        {
                            case ConflictResolutionStrategy.Skip:
                                itemResult.IsSkipped = true;
                                itemResult.Message = "Item already exists, skipped";
                                result.ItemsSkipped++;
                                break;

                            case ConflictResolutionStrategy.Rename:
                                var newName = GetUniqueName(templateItem.Name, categoryItems.Select(i => i.Name).ToList());
                                var newItem = await CreatePackingItem(request.TripId, category.Id, newName, templateItem, request.DefaultAssignedTo);
                                categoryItems.Add(newItem);
                                existingItems.Add(newItem);
                                itemResult.ItemId = newItem.Id;
                                itemResult.IsNew = true;
                                itemResult.Message = $"Renamed to '{newName}' (original already exists)";
                                result.ItemsCreated++;
                                break;

                            case ConflictResolutionStrategy.Overwrite:
                                existingItem.Quantity = templateItem.Quantity;
                                existingItem.IsShared = templateItem.IsShared;
                                if (request.DefaultAssignedTo.HasValue)
                                    existingItem.AssignedTo = request.DefaultAssignedTo.Value;
                                await _packingItemRepository.UpdateAsync(existingItem);
                                itemResult.ItemId = existingItem.Id;
                                itemResult.IsNew = false;
                                itemResult.Message = "Overwritten existing item";
                                break;
                        }
                    }
                    else
                    {
                        var itemName = templateItem.Name;
                        var item = await CreatePackingItem(request.TripId, category.Id, itemName, templateItem, request.DefaultAssignedTo);
                        categoryItems.Add(item);
                        existingItems.Add(item);
                        itemResult.ItemId = item.Id;
                        itemResult.IsNew = true;
                        result.ItemsCreated++;
                    }
                }
                catch (Exception ex)
                {
                    itemResult.IsSkipped = true;
                    itemResult.Message = $"Error: {ex.Message}";
                    result.ItemsFailed++;
                    result.Errors.Add($"Failed to process item '{templateItem.Name}' in category '{templateCategory.Name}': {ex.Message}");
                }

                categoryResult.Items.Add(itemResult);
            }

            result.Details.Add(categoryResult);
        }

        result.PartialSuccess = result.ItemsCreated > 0 || result.CategoriesCreated > 0;
        return result;
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

    private async Task<PackingItem> CreatePackingItem(int tripId, int categoryId, string name, TemplateItemDto templateItem, int? defaultAssignedTo)
    {
        var item = new PackingItem
        {
            TripId = tripId,
            CategoryId = categoryId,
            Name = name,
            Quantity = templateItem.Quantity > 0 ? templateItem.Quantity : 1,
            IsShared = templateItem.IsShared,
            AssignedTo = defaultAssignedTo,
            IsPacked = false,
            CreatedAt = DateTime.UtcNow
        };
        await _packingItemRepository.AddAsync(item);
        return item;
    }

    private static string GetUniqueName(string baseName, List<string> existingNames)
    {
        var counter = 1;
        var newName = $"{baseName} (from template)";
        while (existingNames.Any(n => n.Equals(newName, StringComparison.OrdinalIgnoreCase)))
        {
            counter++;
            newName = $"{baseName} (from template {counter})";
        }
        return newName;
    }

    private static List<TemplateCategoryDto> ParseItemsJson(string itemsJson)
    {
        if (string.IsNullOrWhiteSpace(itemsJson))
            return new List<TemplateCategoryDto>();

        try
        {
            using var doc = JsonDocument.Parse(itemsJson);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Array)
                throw new FormatException("ItemsJson must be a JSON array");

            var categories = new List<TemplateCategoryDto>();
            var defaultCategory = new TemplateCategoryDto { Name = "General", SortOrder = 1 };
            var hasExplicitCategories = false;

            foreach (var element in root.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.String)
                {
                    defaultCategory.Items.Add(new TemplateItemDto
                    {
                        Name = element.GetString() ?? string.Empty,
                        Quantity = 1
                    });
                }
                else if (element.ValueKind == JsonValueKind.Object)
                {
                    if (element.TryGetProperty("items", out var itemsProp) && itemsProp.ValueKind == JsonValueKind.Array)
                    {
                        hasExplicitCategories = true;
                        var categoryName = element.GetProperty("name").GetString()
                            ?? element.GetProperty("category").GetString()
                            ?? "Uncategorized";

                        var sortOrder = element.TryGetProperty("sortOrder", out var sortProp) && sortProp.TryGetInt32(out var sortVal)
                            ? sortVal : 0;

                        var category = new TemplateCategoryDto
                        {
                            Name = categoryName,
                            SortOrder = sortOrder
                        };

                        foreach (var itemElement in itemsProp.EnumerateArray())
                        {
                            category.Items.Add(ParseTemplateItem(itemElement));
                        }

                        categories.Add(category);
                    }
                    else if (element.TryGetProperty("name", out var nameProp))
                    {
                        defaultCategory.Items.Add(ParseTemplateItem(element));
                    }
                }
            }

            if (defaultCategory.Items.Any() && !hasExplicitCategories)
            {
                categories.Insert(0, defaultCategory);
            }

            var sortOrderCounter = 1;
            foreach (var cat in categories.OrderBy(c => c.SortOrder))
            {
                if (cat.SortOrder == 0)
                    cat.SortOrder = sortOrderCounter;
                sortOrderCounter++;
            }

            return categories.OrderBy(c => c.SortOrder).ToList();
        }
        catch (JsonException ex)
        {
            throw new FormatException($"Invalid JSON format in ItemsJson: {ex.Message}", ex);
        }
    }

    private static TemplateItemDto ParseTemplateItem(JsonElement element)
    {
        var item = new TemplateItemDto();

        if (element.ValueKind == JsonValueKind.String)
        {
            item.Name = element.GetString() ?? string.Empty;
            item.Quantity = 1;
        }
        else if (element.ValueKind == JsonValueKind.Object)
        {
            item.Name = element.TryGetProperty("name", out var nameProp)
                ? nameProp.GetString() ?? string.Empty
                : string.Empty;

            item.Quantity = element.TryGetProperty("quantity", out var qtyProp) && qtyProp.TryGetInt32(out var qty)
                ? qty
                : 1;

            item.Description = element.TryGetProperty("description", out var descProp)
                ? descProp.GetString()
                : null;

            item.IsShared = element.TryGetProperty("isShared", out var sharedProp) && sharedProp.GetBoolean();

            item.AssignedToRole = element.TryGetProperty("assignedToRole", out var roleProp)
                ? roleProp.GetString()
                : null;
        }

        return item;
    }
}
