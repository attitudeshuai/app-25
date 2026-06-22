using AutoMapper;
using TripPacking.DTOs;
using TripPacking.Entities;
using TripPacking.Repositories;

namespace TripPacking.Services;

public class TripMemberService : ITripMemberService
{
    private readonly ITripMemberRepository _tripMemberRepository;
    private readonly ITripRepository _tripRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public TripMemberService(ITripMemberRepository tripMemberRepository, ITripRepository tripRepository, IUserRepository userRepository, IMapper mapper)
    {
        _tripMemberRepository = tripMemberRepository;
        _tripRepository = tripRepository;
        _userRepository = userRepository;
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

    private async Task<TripMemberDto> EnrichDto(TripMember member)
    {
        var dto = _mapper.Map<TripMemberDto>(member);
        dto.Role = member.Role.ToString();
        var user = await _userRepository.GetByIdAsync(member.UserId);
        if (user != null)
        {
            dto.Username = user.Username;
            dto.Email = user.Email;
        }
        return dto;
    }

    public async Task<PagedResult<TripMemberDto>> GetPaged(TripMemberQueryDto query, int currentUserId)
    {
        if (query.TripId.HasValue && !await HasTripAccess(query.TripId.Value, currentUserId))
            throw new UnauthorizedAccessException("No access to this trip");

        var pagedResult = await _tripMemberRepository.GetPagedAsync(query.PageIndex, query.PageSize, query.Keyword, query.TripId, query.Role);

        var items = pagedResult.Items;
        var total = pagedResult.Total;

        if (!query.TripId.HasValue)
        {
            var userMemberships = await _tripMemberRepository.GetByUserIdAsync(currentUserId);
            var ownedTrips = await _tripRepository.GetByOwnerIdAsync(currentUserId);
            var accessibleTripIds = userMemberships.Select(m => m.TripId).Concat(ownedTrips.Select(t => t.Id)).ToHashSet();
            items = items.Where(m => accessibleTripIds.Contains(m.TripId)).ToList();
            total = items.Count();
        }

        var dtos = new List<TripMemberDto>();
        foreach (var item in items)
            dtos.Add(await EnrichDto(item));

        return new PagedResult<TripMemberDto> { Items = dtos, Total = total };
    }

    public async Task<TripMemberDto> GetById(int id, int currentUserId)
    {
        var member = await _tripMemberRepository.GetByIdAsync(id);
        if (member == null)
            throw new KeyNotFoundException("Trip member not found");

        if (!await HasTripAccess(member.TripId, currentUserId))
            throw new UnauthorizedAccessException("No access to this trip");

        return await EnrichDto(member);
    }

    public async Task<TripMemberDto> Create(CreateTripMemberDto dto, int currentUserId)
    {
        if (!await IsTripOwner(dto.TripId, currentUserId))
            throw new UnauthorizedAccessException("Only trip owner can add members");

        var user = await _userRepository.GetByIdAsync(dto.UserId);
        if (user == null)
            throw new KeyNotFoundException("User does not exist");

        var existingMembers = await _tripMemberRepository.GetByTripIdAsync(dto.TripId);
        if (existingMembers.Any(m => m.UserId == dto.UserId))
            throw new InvalidOperationException("User is already a member of this trip");

        if (!Enum.TryParse<MemberRole>(dto.Role, true, out var role))
            throw new ArgumentException("Invalid role");

        if (role == MemberRole.Owner && existingMembers.Any(m => m.Role == MemberRole.Owner))
            throw new InvalidOperationException("Trip already has an owner");

        var member = new TripMember
        {
            TripId = dto.TripId,
            UserId = dto.UserId,
            Role = role,
            JoinedAt = DateTime.UtcNow
        };

        await _tripMemberRepository.AddAsync(member);
        return await EnrichDto(member);
    }

    public async Task<TripMemberDto> Update(int id, UpdateTripMemberDto dto, int currentUserId)
    {
        var member = await _tripMemberRepository.GetByIdAsync(id);
        if (member == null)
            throw new KeyNotFoundException("Trip member not found");

        if (!await IsTripOwner(member.TripId, currentUserId))
            throw new UnauthorizedAccessException("Only trip owner can update members");

        if (!string.IsNullOrWhiteSpace(dto.Role) && Enum.TryParse<MemberRole>(dto.Role, true, out var role))
        {
            if (role != member.Role)
            {
                if (member.Role == MemberRole.Owner)
                    throw new InvalidOperationException("Cannot change the owner's role");

                if (role == MemberRole.Owner)
                {
                    var existingMembers = await _tripMemberRepository.GetByTripIdAsync(member.TripId);
                    if (existingMembers.Any(m => m.Role == MemberRole.Owner))
                        throw new InvalidOperationException("Trip already has an owner");
                }
            }

            member.Role = role;
        }

        await _tripMemberRepository.UpdateAsync(member);
        return await EnrichDto(member);
    }

    public async Task Delete(int id, int currentUserId)
    {
        var member = await _tripMemberRepository.GetByIdAsync(id);
        if (member == null)
            throw new KeyNotFoundException("Trip member not found");

        if (!await IsTripOwner(member.TripId, currentUserId))
            throw new UnauthorizedAccessException("Only trip owner can remove members");

        if (member.Role == MemberRole.Owner)
            throw new InvalidOperationException("Cannot remove trip owner");

        await _tripMemberRepository.DeleteAsync(member);
    }

    public async Task<PagedResult<TripMemberDto>> GetMine(int currentUserId, TripMemberQueryDto query)
    {
        var allMemberships = await _tripMemberRepository.GetByUserIdAsync(currentUserId);
        var filtered = allMemberships.AsQueryable();

        if (query.TripId.HasValue)
            filtered = filtered.Where(m => m.TripId == query.TripId.Value);

        if (!string.IsNullOrWhiteSpace(query.Role) && Enum.TryParse<MemberRole>(query.Role, true, out var role))
            filtered = filtered.Where(m => m.Role == role);

        var total = filtered.Count();
        var items = filtered.Skip((query.PageIndex - 1) * query.PageSize).Take(query.PageSize).ToList();

        var dtos = new List<TripMemberDto>();
        foreach (var item in items)
            dtos.Add(await EnrichDto(item));

        return new PagedResult<TripMemberDto> { Items = dtos, Total = total };
    }
}
