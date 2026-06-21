using AutoMapper;
using TripPacking.DTOs;
using TripPacking.Entities;
using TripPacking.Repositories;

namespace TripPacking.Services;

public class PackingTemplateService : IPackingTemplateService
{
    private readonly IPackingTemplateRepository _packingTemplateRepository;
    private readonly IMapper _mapper;

    public PackingTemplateService(IPackingTemplateRepository packingTemplateRepository, IMapper mapper)
    {
        _packingTemplateRepository = packingTemplateRepository;
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
}
