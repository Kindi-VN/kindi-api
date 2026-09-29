using AutoMapper;
using Kindi.API.Application.Common.Exceptions;
using Kindi.API.Application.Errors;
using Kindi.API.Application.Common.Extensions;
using Kindi.API.Application.Common.Helpers;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.Common.Mappings;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Application.Resources;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Domain.Models;
using Kindi.API.Shared.Common.Interfaces;
using Kindi.API.Shared.Exceptions;
using Kindi.API.Shared.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Kindi.API.Application.Services;

public class GroupBuyingRequestService : IGroupBuyingRequestService
{
    private const string AdminRole = "Admin";

    private readonly IRepository<GroupBuyingRequest> _repository;
    private readonly IRepository<GroupBuyingParticipant> _participantRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Collaborator> _collaboratorRepository;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserService _userService;
    private readonly ICollaboratorService _collaboratorService;
    private readonly IQueryService _queryService;
    private readonly IReferralService _referralService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public GroupBuyingRequestService(
        IRepository<GroupBuyingRequest> repository,
        IRepository<GroupBuyingParticipant> participantRepository,
        IRepository<User> userRepository,
        IRepository<Collaborator> collaboratorRepository,
        IMapper mapper,
        ICurrentUserService currentUserService,
        IUserService userService,
        ICollaboratorService collaboratorService,
        IQueryService queryService,
        IReferralService referralService,
        IStringLocalizer<SharedResource> localizer)
    {
        _repository = repository;
        _participantRepository = participantRepository;
        _userRepository = userRepository;
        _collaboratorRepository = collaboratorRepository;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _userService = userService;
        _collaboratorService = collaboratorService;
        _queryService = queryService;
        _referralService = referralService;
        _localizer = localizer;
    }

    // =====================================================================
    // CÔNG KHAI / NGƯỜI DÙNG
    // =====================================================================

    public async Task<GroupBuyingRequestResponseDto> CreateAsync(CreateGroupBuyingRequestDto request)
    {
        // 1. Lấy UserId từ token (nếu có)
        var userId = _currentUserService.UserId;
        var isGuestAccount = false;

        if (string.IsNullOrEmpty(userId))
        {
            // Khách chưa đăng nhập bắt buộc nhập thông tin liên hệ để tạo tài khoản và liên hệ
            if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Phone))
                throw new AppException(GroupBuyingError.ContactRequired);

            if (string.IsNullOrWhiteSpace(request.Email))
                throw new AppException(GroupBuyingError.EmailRequired);

            // 2. Dùng lại tài khoản đã có theo SĐT/email, chỉ tạo mới khi chưa có; thông tin cá nhân
            //    ghi vào bảng Users (không ghi đè SĐT/email của tài khoản đã tồn tại).
            var resolved = await _userService.ResolvePublicUserAsync(request.FullName, request.Phone, request.Email, request.Zalo);
            isGuestAccount = resolved.IsGuestAccount;
            userId = resolved.UserId.ToString();
        }
        else
        {
            // Người đã đăng nhập không phải nhập lại thông tin → bù từ hồ sơ tài khoản
            // để bản ghi luôn có họ tên/SĐT/email cho admin liên hệ.
            var account = await _userService.GetCurrentUserAsync();
            if (account != null)
            {
                if (string.IsNullOrWhiteSpace(request.FullName)) request.FullName = account.FullName;
                if (string.IsNullOrWhiteSpace(request.Phone)) request.Phone = account.Phone ?? string.Empty;
                if (string.IsNullOrWhiteSpace(request.Zalo)) request.Zalo = account.Phone;
                if (string.IsNullOrWhiteSpace(request.Email)) request.Email = account.Email;
            }
        }

        // 3. Map và gán UserId
        var entity = _mapper.Map<GroupBuyingRequest>(request);
        entity.GroupBuyingRequestCode = CodeGenerator.Generate("GBR");
        entity.UserId = Guid.Parse(userId);
        entity.CurrentPeopleCount = 1;
        entity.Status = GroupBuyingStatus.Pending;
        // Mã CTV của link chia sẻ khách dùng để tạo yêu cầu (mã không tồn tại thì bỏ qua)
        entity.ReferralCode = await _referralService.ResolveAsync(request.ReferralCode);

        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();

        // Thông tin cá nhân chỉ lưu ở bảng Users — form gửi lên thì cập nhật vào tài khoản.
        await _userService.UpdatePersonalInfoAsync(entity.UserId, request.FullName, request.Phone, request.Email, request.Zalo);

        // 4. Người mở nhóm chiếm slot đầu tiên → ghi bản ghi participant IsCreator
        //    để danh sách người tham gia và số người của nhóm luôn nhất quán.
        await _participantRepository.AddAsync(new GroupBuyingParticipant
        {
            GroupBuyingParticipantCode = CodeGenerator.Generate("GBPA"),
            GroupBuyingRequestId = entity.Id,
            UserId = entity.UserId,
            Note = request.Note,
            ReferralCode = entity.ReferralCode,
            IsCreator = true,
            IsGuestAccount = isGuestAccount,
            Status = GroupBuyingParticipantStatus.Joined
        });
        await _participantRepository.SaveChangesAsync();

        var dto = _mapper.Map<GroupBuyingRequestResponseDto>(entity);
        var personalInfo = await _userService.GetPersonalInfoAsync(entity.UserId);
        dto.FullName = personalInfo?.FullName ?? string.Empty;
        dto.Phone = personalInfo?.Phone ?? string.Empty;
        dto.Zalo = personalInfo?.Zalo;
        dto.Email = personalInfo?.Email ?? string.Empty;
        await _referralService.FillNamesAsync(new[] { dto }, x => x.ReferralCode, (x, name) => x.ReferralName = name);
        return dto;
    }

    public async Task<PagedList<GroupBuyingFeedItemDto>> GetPublicPagedAsync(GetPublicGroupBuyingRequestsQueryDto query)
    {
        var me = GetCurrentUserId();
        // Từ khoá đã trim + escape; mẫu LIKE được ghép ngay trong biểu thức truy vấn,
        // ILIKE nên tìm không phân biệt hoa/thường.
        var search = query.Search.NormalizeSearchFilter();
        var searchTerm = search?.RemoveVietnameseSign().ToLikeEscaped();

        // Chỉ nhóm đã duyệt (Active) mới lên tab công khai; nhóm của chính mình vẫn thấy
        // (kèm trạng thái "Chờ duyệt") để người tạo theo dõi.
        // Lọc trước rồi mới include: phần join chỉ chạy trên tập bản ghi còn lại
        var q = _queryService.GetQueryableNoTracking<GroupBuyingRequest>()
            .Where(x => x.Status == GroupBuyingStatus.Active
                        || (me != null && x.UserId == me.Value
                            && (x.Status == GroupBuyingStatus.Pending || x.Status == GroupBuyingStatus.Active)))
            .WhereIf(query.MineOnly && me != null, x => x.UserId == me!.Value)
            .WhereIf(!string.IsNullOrEmpty(search), x =>
                EF.Functions.ILike(KindiDbFunctions.Unaccent(x.ProductName), "%" + searchTerm + "%", "\\") ||
                (x.Note != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.Note), "%" + searchTerm + "%", "\\")) ||
                (x.GroupBuyingRequestCode != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.GroupBuyingRequestCode), "%" + searchTerm + "%", "\\")))
            .Include(x => x.User)
            .Include(x => x.BusinessField)
            .Include(x => x.Participants);

        var paged = await q.ToPagedListAsync(
            query.Page, query.PageSize,
            query.SortBy, query.SortOrder,
            defaultSortBy: "CreatedAt");

        var items = paged.Items.Select(x => MapFeedItem(x, me)).ToList();
        return new PagedList<GroupBuyingFeedItemDto>(items, paged.TotalCount, paged.PageNumber, paged.PageSize);
    }

    public async Task<GroupBuyingDetailDto> GetPublicDetailAsync(Guid id)
    {
        var entity = await GetWithParticipantsAsync(id);
        return await MapPublicDetailAsync(entity);
    }

    public async Task<GroupBuyingDetailDto> GetPublicDetailByCodeAsync(string code)
    {
        // So khớp đúng mã (không phân biệt hoa/thường) bằng ILIKE: mẫu là chính từ khoá,
        // không thêm % nên chỉ khớp khi bằng nhau toàn bộ.
        var normalized = code.Trim().RemoveVietnameseSign().ToLikeEscaped();
        var entity = await _repository.GetFirstWithIncludesAsync(
            x => x.GroupBuyingRequestCode != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.GroupBuyingRequestCode), normalized, "\\"),
            includes: q => q.Include(x => x.User)
                .Include(x => x.BusinessField)
                .Include(x => x.Participants).ThenInclude(p => p.User));

        if (entity == null)
            throw new NotFoundException(_localizer["GroupBuyingRequest_NotFound"]);

        return await MapPublicDetailAsync(entity);
    }

    /// <summary>
    /// Kiểm tra quyền xem + map chi tiết cho người dùng thường. Nhóm chưa duyệt / đã đóng
    /// chỉ người mở nhóm (và admin) xem được.
    /// </summary>
    private async Task<GroupBuyingDetailDto> MapPublicDetailAsync(GroupBuyingRequest entity)
    {
        var me = GetCurrentUserId();
        var isAdmin = _currentUserService.IsInRole(AdminRole);

        if (!isAdmin
            && entity.Status != GroupBuyingStatus.Active
            && (me == null || entity.UserId != me.Value))
        {
            throw new NotFoundException(_localizer["GroupBuyingRequest_NotFound"]);
        }

        return await MapDetailAsync(entity, maskContact: ShouldMaskContact(forAdmin: false));
    }

    /// <summary>
    /// Thông tin liên hệ chỉ hiển thị đầy đủ cho admin. Người dùng khác luôn thấy dạng che
    /// để tránh lộ số điện thoại của thành viên qua tài khoản đăng ký ảo.
    /// </summary>
    private bool ShouldMaskContact(bool forAdmin)
        => !forAdmin || !_currentUserService.IsInRole(AdminRole);

    public async Task<JoinGroupBuyingResponseDto> JoinAsync(Guid id, JoinGroupBuyingRequestDto request)
    {
        var entity = await GetWithParticipantsAsync(id);

        if (entity.Status == GroupBuyingStatus.Pending)
            throw new AppException(GroupBuyingError.NotApprovedYet);

        if (entity.Status != GroupBuyingStatus.Active)
            throw new AppException(GroupBuyingError.NotOpen);

        var currentUserId = GetCurrentUserId();
        Guid userId;
        var isNewAccount = false;
        var isGuestAccount = false;
        var accountAlreadyExisted = false;

        if (currentUserId != null)
        {
            userId = currentUserId.Value;
        }
        else
        {
            // Khách chưa đăng nhập → bắt buộc có họ tên + số điện thoại để tạo tài khoản và liên hệ
            if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Phone))
                throw new AppException(GroupBuyingError.ContactRequired);

            var resolved = await _userService.ResolvePublicUserAsync(request.FullName, request.Phone, request.Email, request.Zalo);
            userId = resolved.UserId;
            isNewAccount = resolved.IsNewAccount;
            accountAlreadyExisted = !resolved.IsNewAccount;
            isGuestAccount = resolved.IsGuestAccount;

            // Mọi người tham gia mua chung đều được lưu ở bảng Collaborators (trạng thái chờ duyệt)
            await _collaboratorService.EnsureProfileForUserAsync(userId);
        }

        if (entity.UserId == userId)
            throw new AppException(GroupBuyingError.CreatorCannotJoin);

        var participant = entity.Participants.FirstOrDefault(p => p.UserId == userId);
        if (participant != null && participant.Status == GroupBuyingParticipantStatus.Joined)
            throw new AppException(GroupBuyingError.AlreadyJoined);

        // Thông tin liên hệ chỉ lưu ở bảng Users — khách điền thì cập nhật vào tài khoản của họ.
        await _userService.UpdatePersonalInfoAsync(userId, request.FullName, request.Phone, request.Email, request.Zalo);

        // Mã CTV của link chia sẻ người này dùng để tham gia (mã không tồn tại thì bỏ qua)
        var referralCode = await _referralService.ResolveAsync(request.ReferralCode);

        if (participant == null)
        {
            participant = new GroupBuyingParticipant
            {
                GroupBuyingParticipantCode = CodeGenerator.Generate("GBPA"),
                GroupBuyingRequestId = entity.Id,
                UserId = userId,
                Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
                ReferralCode = referralCode,
                IsCreator = false,
                IsGuestAccount = isGuestAccount,
                Status = GroupBuyingParticipantStatus.Joined
            };

            await _participantRepository.AddAsync(participant);
        }
        else
        {
            // Đã từng hủy tham gia → tái kích hoạt bản ghi cũ (tránh phá unique index)
            participant.Status = GroupBuyingParticipantStatus.Joined;
            participant.UpdatedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(request.Note)) participant.Note = request.Note.Trim();
            if (referralCode != null) participant.ReferralCode = referralCode;
            _participantRepository.Update(participant);
        }

        await _participantRepository.SaveChangesAsync();
        var currentCount = await SyncPeopleCountAsync(entity);

        string? username = null;
        if (isNewAccount)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            username = user?.Username;
        }

        var needed = Math.Max(0, entity.TargetPeopleCount - currentCount);

        return new JoinGroupBuyingResponseDto
        {
            ParticipantId = participant.Id,
            CurrentPeopleCount = currentCount,
            TargetPeopleCount = entity.TargetPeopleCount,
            NeededPeopleCount = needed,
            IsGroupFull = needed == 0,
            IsNewAccount = isNewAccount,
            AccountAlreadyExisted = accountAlreadyExisted,
            Username = username,
            PasswordIsPhone = isNewAccount,
            Message = isNewAccount
                ? _localizer["GroupBuyingRequest_JoinSuccessNewAccount"]
                : _localizer["GroupBuyingRequest_JoinSuccess"]
        };
    }

    public async Task<GroupBuyingDetailDto> LeaveAsync(Guid id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            throw new AppException(GroupBuyingError.LoginRequired);

        var entity = await GetWithParticipantsAsync(id);
        var participant = entity.Participants.FirstOrDefault(p => p.UserId == currentUserId.Value);

        if (participant == null || participant.Status != GroupBuyingParticipantStatus.Joined)
            throw new AppException(GroupBuyingError.NotParticipant);

        if (participant.IsCreator)
            throw new AppException(GroupBuyingError.CreatorCannotLeave);

        participant.Status = GroupBuyingParticipantStatus.Cancelled;
        participant.UpdatedAt = DateTime.UtcNow;
        _participantRepository.Update(participant);
        await _participantRepository.SaveChangesAsync();

        await SyncPeopleCountAsync(entity);
        return await MapDetailAsync(entity, maskContact: ShouldMaskContact(forAdmin: false));
    }

    // =====================================================================
    // ADMIN
    // =====================================================================

    public async Task<PagedList<GroupBuyingRequestResponseDto>> GetPagedAsync(GetGroupBuyingRequestsQueryDto query)
    {
        // Admin - lấy tất cả; User - chỉ lấy của mình
        var isAdmin = _currentUserService.IsInRole(AdminRole);
        var userId = isAdmin ? null : _currentUserService.UserId;

        if (!isAdmin && string.IsNullOrEmpty(userId))
            return new PagedList<GroupBuyingRequestResponseDto>(new List<GroupBuyingRequestResponseDto>(), 0, query.Page, query.PageSize);

        // Từ khoá đã trim + escape; mẫu LIKE được ghép ngay trong biểu thức truy vấn,
        // ILIKE nên tìm không phân biệt hoa/thường.
        var search = query.Search.NormalizeSearchFilter();
        var searchTerm = search?.RemoveVietnameseSign().ToLikeEscaped();

        var statusFilter = GroupBuyingStatus.Pending;
        var normalizedStatus = query.Status.NormalizeSearchFilter();
        var hasStatusFilter = !string.IsNullOrEmpty(normalizedStatus)
            && Enum.TryParse(normalizedStatus, true, out statusFilter);

        // Lọc trước rồi mới include
        var q = _queryService.GetQueryableNoTracking<GroupBuyingRequest>()
            .WhereIf(userId != null, x => x.UserId == Guid.Parse(userId!))
            .WhereIf(hasStatusFilter, x => x.Status == statusFilter)
            .WhereIf(!string.IsNullOrEmpty(search), x =>
                (x.GroupBuyingRequestCode != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.GroupBuyingRequestCode), "%" + searchTerm + "%", "\\")) ||
                EF.Functions.ILike(KindiDbFunctions.Unaccent(x.ProductName), "%" + searchTerm + "%", "\\") ||
                EF.Functions.ILike(KindiDbFunctions.Unaccent(x.User.FullName), "%" + searchTerm + "%", "\\") ||
                (x.User.Phone != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.User.Phone), "%" + searchTerm + "%", "\\")) ||
                (x.User.Email != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.User.Email), "%" + searchTerm + "%", "\\")))
            .Include(x => x.User)
            .Include(x => x.BusinessField);

        var result = await q.ToPagedListAsync(
            query.Page, query.PageSize,
            query.SortBy, query.SortOrder,
            defaultSortBy: "CreatedAt");

        var paged = _mapper.MapPagedList<GroupBuyingRequest, GroupBuyingRequestResponseDto>(result);
        await _referralService.FillNamesAsync(paged.Items, x => x.ReferralCode, (x, name) => x.ReferralName = name);
        return paged;
    }

    public async Task<GroupBuyingDetailDto> GetDetailAsync(Guid id)
    {
        var entity = await GetWithParticipantsAsync(id);
        return await MapDetailAsync(entity, maskContact: ShouldMaskContact(forAdmin: true));
    }

    public async Task<GroupBuyingRequestResponseDto> UpdateStatusAsync(Guid id, UpdateGroupBuyingStatusDto request)
    {
        if (!Enum.IsDefined(typeof(GroupBuyingStatus), request.Status))
            throw new AppException(GroupBuyingError.InvalidStatus);

        var entity = await GetWithParticipantsAsync(id);
        var nextStatus = (GroupBuyingStatus)request.Status;

        if (nextStatus != entity.Status)
        {
            EnsureStatusTransition(entity.Status, nextStatus);

            entity.Status = nextStatus;

            if (nextStatus == GroupBuyingStatus.Active)
            {
                entity.ApprovedAt = DateTime.UtcNow;
                entity.ApprovedByUserId = GetCurrentUserId();
                entity.ClosedReason = null;
            }

            if (nextStatus == GroupBuyingStatus.Completed || nextStatus == GroupBuyingStatus.Cancelled)
                entity.ClosedReason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();

            _repository.Update(entity);
            await _repository.SaveChangesAsync();
        }

        var dto = _mapper.Map<GroupBuyingRequestResponseDto>(entity);
        var personalInfo = await _userService.GetPersonalInfoAsync(entity.UserId);
        dto.FullName = personalInfo?.FullName ?? string.Empty;
        dto.Phone = personalInfo?.Phone ?? string.Empty;
        dto.Zalo = personalInfo?.Zalo;
        dto.Email = personalInfo?.Email ?? string.Empty;
        await _referralService.FillNamesAsync(new[] { dto }, x => x.ReferralCode, (x, name) => x.ReferralName = name);
        return dto;
    }

    public async Task<GroupBuyingRequestResponseDto> UpdateAsync(Guid id, UpdateGroupBuyingRequestDto request)
    {
        var entity = await GetWithParticipantsAsync(id);

        if (!string.IsNullOrWhiteSpace(request.ProductName))
            entity.ProductName = request.ProductName.Trim();

        if (request.ProductLink != null)
            entity.ProductLink = string.IsNullOrWhiteSpace(request.ProductLink) ? null : request.ProductLink.Trim();

        if (request.Note != null)
            entity.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

        if (request.TargetPrice.HasValue)
            entity.TargetPrice = request.TargetPrice.Value;

        if (request.TargetPeopleCount.HasValue)
        {
            if (request.TargetPeopleCount.Value < entity.CurrentPeopleCount)
                throw new AppException(GroupBuyingError.TargetLessThanCurrent);

            entity.TargetPeopleCount = request.TargetPeopleCount.Value;
        }

        _repository.Update(entity);
        await _repository.SaveChangesAsync();

        var dto = _mapper.Map<GroupBuyingRequestResponseDto>(entity);
        var personalInfo = await _userService.GetPersonalInfoAsync(entity.UserId);
        dto.FullName = personalInfo?.FullName ?? string.Empty;
        dto.Phone = personalInfo?.Phone ?? string.Empty;
        dto.Zalo = personalInfo?.Zalo;
        dto.Email = personalInfo?.Email ?? string.Empty;
        await _referralService.FillNamesAsync(new[] { dto }, x => x.ReferralCode, (x, name) => x.ReferralName = name);
        return dto;
    }

    public async Task<GroupBuyingDetailDto> RemoveParticipantAsync(Guid id, Guid participantId)
    {
        var entity = await GetWithParticipantsAsync(id);
        var participant = entity.Participants.FirstOrDefault(p => p.Id == participantId);

        if (participant == null)
            throw new NotFoundException(_localizer["GroupBuyingRequest_ParticipantNotFound"]);

        if (participant.IsCreator)
            throw new AppException(GroupBuyingError.CannotRemoveCreator);

        participant.Status = GroupBuyingParticipantStatus.Cancelled;
        participant.UpdatedAt = DateTime.UtcNow;
        _participantRepository.Update(participant);
        await _participantRepository.SaveChangesAsync();

        await SyncPeopleCountAsync(entity);
        return await MapDetailAsync(entity, maskContact: ShouldMaskContact(forAdmin: true));
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null)
            throw new NotFoundException(_localizer["GroupBuyingRequest_NotFound"]);

        _repository.Delete(entity);
        await _repository.SaveChangesAsync();
    }

    // =====================================================================
    // HELPERS
    // =====================================================================

    private Guid? GetCurrentUserId()
        => string.IsNullOrEmpty(_currentUserService.UserId)
            ? null
            : Guid.Parse(_currentUserService.UserId!);

    private async Task<GroupBuyingRequest> GetWithParticipantsAsync(Guid id)
    {
        var entity = await _repository.GetFirstWithIncludesAsync(
            x => x.Id == id,
            includes: q => q.Include(x => x.User)
                .Include(x => x.BusinessField)
                .Include(x => x.Participants).ThenInclude(p => p.User));

        if (entity == null)
            throw new NotFoundException(_localizer["GroupBuyingRequest_NotFound"]);

        return entity;
    }

    /// <summary>Đồng bộ cột CurrentPeopleCount = 1 (người mở nhóm) + số người đang tham gia.</summary>
    private async Task<int> SyncPeopleCountAsync(GroupBuyingRequest entity)
    {
        var joinedOthers = await _queryService.CountAsync<GroupBuyingParticipant>(p =>
            p.GroupBuyingRequestId == entity.Id
            && p.Status == GroupBuyingParticipantStatus.Joined
            && !p.IsCreator);

        var total = 1 + joinedOthers;
        if (entity.CurrentPeopleCount != total)
        {
            entity.CurrentPeopleCount = total;
            _repository.Update(entity);
            await _repository.SaveChangesAsync();
        }

        return total;
    }

    private void EnsureStatusTransition(GroupBuyingStatus current, GroupBuyingStatus next)
    {
        var allowed = current switch
        {
            GroupBuyingStatus.Pending => new[] { GroupBuyingStatus.Active, GroupBuyingStatus.Cancelled },
            GroupBuyingStatus.Active => new[] { GroupBuyingStatus.Completed, GroupBuyingStatus.Cancelled },
            GroupBuyingStatus.Cancelled => new[] { GroupBuyingStatus.Active },
            GroupBuyingStatus.Completed => Array.Empty<GroupBuyingStatus>(),
            _ => Array.Empty<GroupBuyingStatus>()
        };

        if (!allowed.Contains(next))
        {
            throw new AppException(GroupBuyingError.InvalidStatusTransition.WithParams(current, next));
        }
    }

    private static int ComputeCurrentPeopleCount(GroupBuyingRequest entity, ICollection<GroupBuyingParticipant> joined)
        => 1 + joined.Count(p => !p.IsCreator);

    private static GroupBuyingFeedItemDto MapFeedItem(GroupBuyingRequest entity, Guid? me)
    {
        var joined = entity.Participants
            .Where(p => p.Status == GroupBuyingParticipantStatus.Joined)
            .ToList();

        var current = ComputeCurrentPeopleCount(entity, joined);

        return new GroupBuyingFeedItemDto
        {
            Id = entity.Id,
            GroupBuyingRequestCode = entity.GroupBuyingRequestCode,
            ProductName = entity.ProductName,
            ProductLink = entity.ProductLink,
            TargetPrice = entity.TargetPrice,
            TargetPeopleCount = entity.TargetPeopleCount,
            CurrentPeopleCount = current,
            NeededPeopleCount = Math.Max(0, entity.TargetPeopleCount - current),
            Status = entity.Status,
            Note = entity.Note,
            BusinessFieldName = entity.BusinessField?.Name,
            CreatorName = entity.User?.FullName ?? string.Empty,
            CreatedAt = entity.CreatedAt,
            IsMine = me.HasValue && entity.UserId == me.Value,
            IsJoinedByMe = me.HasValue && joined.Any(p => p.UserId == me.Value),
            CanJoin = entity.Status == GroupBuyingStatus.Active && me != entity.UserId,
            Participants = joined
                .Where(p => !p.IsCreator)
                .OrderBy(p => p.CreatedAt)
                .Select(p => MaskName(p.User?.FullName))
                .Take(6)
                .ToList()
        };
    }

    private async Task<GroupBuyingDetailDto> MapDetailAsync(GroupBuyingRequest entity, bool maskContact)
    {
        var me = GetCurrentUserId();
        var joined = entity.Participants
            .Where(p => p.Status == GroupBuyingParticipantStatus.Joined)
            .ToList();

        var current = ComputeCurrentPeopleCount(entity, joined);
        var needed = Math.Max(0, entity.TargetPeopleCount - current);
        var isMine = me.HasValue && entity.UserId == me.Value;
        var isJoinedByMe = me.HasValue && joined.Any(p => p.UserId == me.Value);

        var collaboratorCodes = await LoadCollaboratorCodesAsync(entity.Participants.Select(p => p.UserId));

        // Tên CTV của các mã ghi nhận được ở yêu cầu + từng người tham gia (hiển thị ở màn quản trị).
        var referralNames = await _referralService.LoadNamesAsync(
            entity.Participants.Select(p => p.ReferralCode).Append(entity.ReferralCode));

        // Chỉ admin xem được liên hệ đầy đủ; người dùng khác (kể cả người mở nhóm) chỉ thấy
        // liên hệ của chính mình để tránh lộ số điện thoại qua tài khoản đăng ký ảo.
        bool MaskContactOf(Guid rowUserId) => maskContact && !(me.HasValue && rowUserId == me.Value);

        // Email tạm hệ thống sinh cho tài khoản tự động ({sđt}@temp.com) coi như chưa có email.
        static string? DisplayEmailOf(User? user) => UserInfo.DisplayEmail(user?.Email, user?.Phone);

        // Chỉ trả về người đang tham gia: người đã gỡ (Status = Cancelled) không hiện lại ở
        // bảng người tham gia của màn quản trị; số lượng cũng tính theo danh sách này.
        var participants = entity.Participants
            .Where(p => p.Status == GroupBuyingParticipantStatus.Joined)
            .OrderByDescending(p => p.IsCreator)
            .ThenBy(p => p.CreatedAt)
            .Select(p => new GroupBuyingParticipantDto
            {
                Id = p.Id,
                GroupBuyingParticipantCode = p.GroupBuyingParticipantCode,
                UserId = p.UserId,
                UserCode = p.User?.UserCode,
                CollaboratorCode = collaboratorCodes.TryGetValue(p.UserId, out var code) ? code : null,
                FullName = p.User?.FullName ?? string.Empty,
                Phone = MaskContactOf(p.UserId) && !string.IsNullOrEmpty(p.User?.Phone) ? MaskPhone(p.User!.Phone) : p.User?.Phone ?? string.Empty,
                Zalo = MaskContactOf(p.UserId) && !string.IsNullOrEmpty(p.User?.Zalo) ? MaskPhone(p.User!.Zalo) : p.User?.Zalo,
                Email = MaskContactOf(p.UserId) && !string.IsNullOrEmpty(DisplayEmailOf(p.User))
                    ? MaskEmail(DisplayEmailOf(p.User))
                    : DisplayEmailOf(p.User),
                Note = p.Note,
                ReferralCode = p.ReferralCode,
                ReferralName = p.ReferralCode != null && referralNames.TryGetValue(p.ReferralCode, out var referralName)
                    ? referralName
                    : null,
                IsCreator = p.IsCreator,
                IsGuestAccount = p.IsGuestAccount,
                Status = p.Status,
                IsMe = me.HasValue && p.UserId == me.Value,
                CreatedAt = p.CreatedAt
            })
            .ToList();

        // Lý do không thể tham gia (để UI hiển thị nút mờ + tooltip)
        var canJoin = entity.Status == GroupBuyingStatus.Active && !isMine && !isJoinedByMe;
        string? blockedReason = null;
        if (!canJoin)
        {
            if (isMine) blockedReason = _localizer["GroupBuyingRequest_YouAreCreator"];
            else if (isJoinedByMe) blockedReason = _localizer["GroupBuyingRequest_AlreadyJoined"];
            else if (entity.Status == GroupBuyingStatus.Pending) blockedReason = _localizer["GroupBuyingRequest_NotApprovedYet"];
            else if (entity.Status == GroupBuyingStatus.Cancelled) blockedReason = _localizer["GroupBuyingRequest_Cancelled"];
            else if (entity.Status == GroupBuyingStatus.Completed) blockedReason = _localizer["GroupBuyingRequest_Completed"];
        }

        return new GroupBuyingDetailDto
        {
            Id = entity.Id,
            GroupBuyingRequestCode = entity.GroupBuyingRequestCode,
            ProductName = entity.ProductName,
            ProductLink = entity.ProductLink,
            TargetPrice = entity.TargetPrice,
            TargetPeopleCount = entity.TargetPeopleCount,
            CurrentPeopleCount = current,
            NeededPeopleCount = needed,
            Status = entity.Status,
            Note = entity.Note,
            BusinessFieldId = entity.BusinessFieldId,
            BusinessFieldName = entity.BusinessField?.Name,
            CreatedAt = entity.CreatedAt,
            ApprovedAt = entity.ApprovedAt,
            ClosedReason = entity.ClosedReason,
            ReferralCode = entity.ReferralCode,
            ReferralName = entity.ReferralCode != null && referralNames.TryGetValue(entity.ReferralCode, out var requestReferralName)
                ? requestReferralName
                : null,
            CreatorName = entity.User?.FullName ?? string.Empty,
            CreatorPhone = MaskContactOf(entity.UserId) && !string.IsNullOrEmpty(entity.User?.Phone) ? MaskPhone(entity.User!.Phone) : entity.User?.Phone ?? string.Empty,
            CreatorZalo = MaskContactOf(entity.UserId) && !string.IsNullOrEmpty(entity.User?.Zalo) ? MaskPhone(entity.User!.Zalo) : entity.User?.Zalo,
            CreatorEmail = MaskContactOf(entity.UserId) && !string.IsNullOrEmpty(DisplayEmailOf(entity.User))
                ? MaskEmail(DisplayEmailOf(entity.User))
                : DisplayEmailOf(entity.User),
            IsMine = isMine,
            IsJoinedByMe = isJoinedByMe,
            CanJoin = canJoin,
            JoinBlockedReason = blockedReason,
            Participants = participants
        };
    }

    private async Task<Dictionary<Guid, string>> LoadCollaboratorCodesAsync(IEnumerable<Guid> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<Guid, string>();

        var codes = await _collaboratorRepository.GetQueryable()
            .Where(c => ids.Contains(c.UserId) && c.CollaboratorCode != null)
            .Select(c => new { c.UserId, c.CollaboratorCode })
            .ToListAsync();

        return codes
            .GroupBy(c => c.UserId)
            .ToDictionary(g => g.Key, g => g.First().CollaboratorCode!);
    }

    private static string MaskName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "***";

        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return parts[0];
        if (parts.Length == 2) return $"{parts[0]} {parts[1][..1]}.";

        return $"{parts[0]} *** {parts[^1]}";
    }

    private static string MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return "***";
        return phone.Length < 7 ? "***" : $"{phone[..3]}***{phone[^3..]}";
    }

    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "***";

        var at = email.IndexOf('@');
        return at <= 1 ? "***" : $"{email[0]}***{email[at..]}";
    }
}
