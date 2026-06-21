using AutoMapper;
using TripPacking.DTOs;
using TripPacking.Entities;
using TripPacking.Repositories;

namespace TripPacking.Services;

public class InvitationService : IInvitationService
{
    private readonly IInvitationRepository _invitationRepository;
    private readonly ITripRepository _tripRepository;
    private readonly ITripMemberRepository _tripMemberRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;
    private readonly IMapper _mapper;

    public InvitationService(
        IInvitationRepository invitationRepository,
        ITripRepository tripRepository,
        ITripMemberRepository tripMemberRepository,
        IUserRepository userRepository,
        INotificationService notificationService,
        IMapper mapper)
    {
        _invitationRepository = invitationRepository;
        _tripRepository = tripRepository;
        _tripMemberRepository = tripMemberRepository;
        _userRepository = userRepository;
        _notificationService = notificationService;
        _mapper = mapper;
    }

    private async Task<bool> IsTripOwner(int tripId, int userId)
    {
        var trip = await _tripRepository.GetByIdAsync(tripId);
        return trip != null && trip.OwnerId == userId;
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

    private InvitationDto EnrichDto(Invitation invitation)
    {
        var dto = _mapper.Map<InvitationDto>(invitation);
        dto.Role = invitation.Role.ToString();
        dto.Status = invitation.Status.ToString();

        if (invitation.Trip != null)
        {
            dto.TripTitle = invitation.Trip.Title;
            dto.TripDestination = invitation.Trip.Destination;
        }

        if (invitation.InvitedBy != null)
        {
            dto.InvitedByUsername = invitation.InvitedBy.Username;
            dto.InvitedByAvatar = invitation.InvitedBy.Avatar;
        }

        if (invitation.InvitedUser != null)
        {
            dto.InvitedUserUsername = invitation.InvitedUser.Username;
            dto.InvitedUserEmail = invitation.InvitedUser.Email;
        }

        return dto;
    }

    public async Task<InvitationDto> CreateAsync(CreateInvitationDto dto, int currentUserId)
    {
        if (!await IsTripOwner(dto.TripId, currentUserId))
            throw new UnauthorizedAccessException("Only trip owner can send invitations");

        var invitedUser = await _userRepository.GetByIdAsync(dto.InvitedUserId);
        if (invitedUser == null)
            throw new KeyNotFoundException("Invited user not found");

        var existingMembers = await _tripMemberRepository.GetByTripIdAsync(dto.TripId);
        if (existingMembers.Any(m => m.UserId == dto.InvitedUserId))
            throw new InvalidOperationException("User is already a member of this trip");

        var existingPending = await _invitationRepository.GetPendingByTripAndUserAsync(dto.TripId, dto.InvitedUserId);
        if (existingPending != null)
            throw new InvalidOperationException("A pending invitation already exists for this user");

        if (!Enum.TryParse<MemberRole>(dto.Role, true, out var role))
            throw new ArgumentException("Invalid role");

        if (role == MemberRole.Owner)
            throw new ArgumentException("Cannot invite someone as Owner");

        var expiryHours = dto.ExpiryHours ?? 72;
        if (expiryHours <= 0)
            throw new ArgumentException("Expiry hours must be positive");

        var trip = await _tripRepository.GetByIdAsync(dto.TripId);

        var invitation = new Invitation
        {
            TripId = dto.TripId,
            InvitedById = currentUserId,
            InvitedUserId = dto.InvitedUserId,
            Role = role,
            Status = InvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddHours(expiryHours),
            Message = dto.Message,
            CreatedAt = DateTime.UtcNow
        };

        await _invitationRepository.AddAsync(invitation);

        var inviter = await _userRepository.GetByIdAsync(currentUserId);
        await _notificationService.SendInvitationReceivedAsync(
            dto.InvitedUserId,
            trip!,
            inviter!,
            invitation.Id,
            dto.Message);

        return await GetByIdAsync(invitation.Id, currentUserId);
    }

    public async Task<InvitationDto> GetByIdAsync(int id, int currentUserId)
    {
        var invitation = await _invitationRepository.GetByIdAsync(id);
        if (invitation == null)
            throw new KeyNotFoundException("Invitation not found");

        if (invitation.InvitedById != currentUserId &&
            invitation.InvitedUserId != currentUserId &&
            !await HasTripAccess(invitation.TripId, currentUserId))
            throw new UnauthorizedAccessException("No access to this invitation");

        return EnrichDto(invitation);
    }

    public async Task<PagedResult<InvitationDto>> GetPagedAsync(InvitationQueryDto query, int currentUserId)
    {
        InvitationStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<InvitationStatus>(query.Status, true, out var parsedStatus))
            status = parsedStatus;

        var pagedResult = await _invitationRepository.GetPagedAsync(
            query.PageIndex, query.PageSize, query.TripId, status, query.Direction, currentUserId);

        var dtos = new List<InvitationDto>();
        foreach (var item in pagedResult.Items)
            dtos.Add(EnrichDto(item));

        return new PagedResult<InvitationDto> { Items = dtos, Total = pagedResult.Total };
    }

    public async Task<InvitationDto> RespondAsync(int id, RespondInvitationDto dto, int currentUserId)
    {
        var invitation = await _invitationRepository.GetByIdAsync(id);
        if (invitation == null)
            throw new KeyNotFoundException("Invitation not found");

        if (invitation.InvitedUserId != currentUserId)
            throw new UnauthorizedAccessException("Only the invited user can respond");

        if (invitation.Status != InvitationStatus.Pending)
            throw new InvalidOperationException("This invitation has already been processed");

        if (invitation.ExpiresAt <= DateTime.UtcNow)
        {
            invitation.Status = InvitationStatus.Expired;
            invitation.RespondedAt = DateTime.UtcNow;
            await _invitationRepository.UpdateAsync(invitation);
            throw new InvalidOperationException("This invitation has expired");
        }

        invitation.RespondedAt = DateTime.UtcNow;

        if (dto.Accept)
        {
            var existingMember = await _tripMemberRepository.GetByTripAndUserIdAsync(invitation.TripId, invitation.InvitedUserId);
            if (existingMember != null)
            {
                invitation.Status = InvitationStatus.Accepted;
                await _invitationRepository.UpdateAsync(invitation);
            }
            else
            {
                var member = new TripMember
                {
                    TripId = invitation.TripId,
                    UserId = invitation.InvitedUserId,
                    Role = invitation.Role,
                    JoinedAt = DateTime.UtcNow
                };
                await _tripMemberRepository.AddAsync(member);

                invitation.Status = InvitationStatus.Accepted;
                await _invitationRepository.UpdateAsync(invitation);

                var invitedUser = await _userRepository.GetByIdAsync(currentUserId);
                var trip = await _tripRepository.GetByIdAsync(invitation.TripId);
                await _notificationService.SendInvitationAcceptedAsync(
                    invitation.InvitedById,
                    trip!,
                    invitedUser!,
                    invitation.Id);
                await _notificationService.SendMemberJoinedAsync(
                    invitation.Trip.OwnerId,
                    trip!,
                    invitedUser!,
                    invitation.TripId);
            }
        }
        else
        {
            invitation.Status = InvitationStatus.Rejected;
            await _invitationRepository.UpdateAsync(invitation);

            var invitedUser = await _userRepository.GetByIdAsync(currentUserId);
            var trip = await _tripRepository.GetByIdAsync(invitation.TripId);
            await _notificationService.SendInvitationRejectedAsync(
                invitation.InvitedById,
                trip!,
                invitedUser!,
                invitation.Id);
        }

        return EnrichDto(invitation);
    }

    public async Task CancelAsync(int id, int currentUserId)
    {
        var invitation = await _invitationRepository.GetByIdAsync(id);
        if (invitation == null)
            throw new KeyNotFoundException("Invitation not found");

        if (invitation.InvitedById != currentUserId && !await IsTripOwner(invitation.TripId, currentUserId))
            throw new UnauthorizedAccessException("Only the inviter or trip owner can cancel");

        if (invitation.Status != InvitationStatus.Pending)
            throw new InvalidOperationException("Can only cancel pending invitations");

        invitation.Status = InvitationStatus.Cancelled;
        invitation.RespondedAt = DateTime.UtcNow;
        await _invitationRepository.UpdateAsync(invitation);

        var trip = await _tripRepository.GetByIdAsync(invitation.TripId);
        await _notificationService.SendInvitationCancelledAsync(
            invitation.InvitedUserId,
            trip!,
            invitation.Id);
    }

    public async Task<int> ExpireInvitationsAsync()
    {
        var expired = await _invitationRepository.GetExpiredPendingAsync();
        var count = 0;

        foreach (var invitation in expired)
        {
            invitation.Status = InvitationStatus.Expired;
            invitation.RespondedAt = DateTime.UtcNow;
            await _invitationRepository.UpdateAsync(invitation);
            count++;

            var trip = await _tripRepository.GetByIdAsync(invitation.TripId);
            await _notificationService.SendInvitationExpiredAsync(
                invitation.InvitedById,
                trip!,
                invitation.InvitedUser,
                invitation.Id);
        }

        return count;
    }
}
