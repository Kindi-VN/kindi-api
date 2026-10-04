using AutoMapper;
using Kindi.API.Application.Common.Configurations;
using Kindi.API.Application.Common.Exceptions;
using Kindi.API.Application.Common.Extensions;
using Kindi.API.Application.Common.Helpers;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.Common.Mappings;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.Responses;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Application.Resources;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Domain.Models;
using Kindi.API.Shared.Common.Helpers;
using Kindi.API.Shared.Common.Interfaces;
using Kindi.API.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Kindi.API.Application.Services;

/// <summary>
/// Nhóm theo lĩnh vực kinh doanh: gom đối tác cùng lĩnh vực để admin gửi offer / yêu cầu
/// mua chung / yêu cầu tìm nhà cung cấp và thành viên trao đổi như một bài post.
/// </summary>
public class BusinessGroupService : IBusinessGroupService
{

    private readonly IRepository<BusinessGroup> _repository;
    private readonly IRepository<BusinessGroupMember> _memberRepository;
    private readonly IRepository<BusinessGroupPost> _postRepository;
    private readonly IRepository<BusinessGroupComment> _commentRepository;
    private readonly ICollaboratorService _collaboratorService;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserService _userService;
    private readonly IQueryService _queryService;
    private readonly IReferralService _referralService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly QueryOptions _queryOptions;

    public BusinessGroupService(
        IRepository<BusinessGroup> repository,
        IRepository<BusinessGroupMember> memberRepository,
        IRepository<BusinessGroupPost> postRepository,
        IRepository<BusinessGroupComment> commentRepository,
        ICollaboratorService collaboratorService,
        IMapper mapper,
        ICurrentUserService currentUserService,
        IUserService userService,
        IQueryService queryService,
        IReferralService referralService,
        IStringLocalizer<SharedResource> localizer,
        IOptions<QueryOptions> queryOptions)
    {
        _repository = repository;
        _memberRepository = memberRepository;
        _postRepository = postRepository;
        _commentRepository = commentRepository;
        _collaboratorService = collaboratorService;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _userService = userService;
        _queryService = queryService;
        _referralService = referralService;
        _localizer = localizer;
        _queryOptions = queryOptions.Value;
    }

    // =====================================================================
    // CÔNG KHAI
    // =====================================================================

    public async Task<PagedList<BusinessGroupResponseDto>> GetPublicPagedAsync(BusinessGroupQueryDto query)
    {
        var me = GetCurrentUserId();
        var search = query.Search.NormalizeSearchFilter();
        // Từ khoá đã trim + escape; mẫu LIKE được ghép ngay trong biểu thức truy vấn,
        // ILIKE nên tìm không phân biệt hoa/thường.
        var searchTerm = search?.RemoveVietnameseSign().ToLikeEscaped();

        var q = _queryService.GetAllNoTracking<BusinessGroup>()
            // Trang Nhóm ngành chỉ hiển thị nhóm ngành (hội nhóm có danh sách riêng)
            .Where(x => x.Type == BusinessGroupType.Industry && x.IsActive)
            .WhereIf(query.BusinessFieldId.HasValue, x => x.BusinessFieldId == query.BusinessFieldId!.Value)
            .WhereIf(!string.IsNullOrEmpty(search), x =>
                EF.Functions.ILike(KindiDbFunctions.Unaccent(x.Name), "%" + searchTerm + "%", "\\") ||
                (x.Description != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.Description), "%" + searchTerm + "%", "\\")) ||
                (x.BusinessFieldName != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.BusinessFieldName), "%" + searchTerm + "%", "\\")))
            .WhereIf(query.MineOnly && me != null,
                x => x.Members.Any(m => m.UserId == me!.Value && m.Status == GroupMemberStatus.Active))
            .Include(x => x.BusinessField);

        var paged = await q.ToPagedListAsync(query.Page, query.PageSize, null, null, defaultSortBy: "CreatedAt");
        var result = _mapper.MapPagedList<BusinessGroup, BusinessGroupResponseDto>(paged);

        await ApplyViewerStateAsync(result.Items, me, includePendingCounts: false);
        return result;
    }

    public async Task<BusinessGroupDetailDto> GetPublicByIdAsync(Guid id)
    {
        var me = GetCurrentUserId();
        var isAdmin = _currentUserService.IsInRole(UserRole.Admin);

        var group = await _queryService.GetAllNoTracking<BusinessGroup>()
            .Include(x => x.BusinessField)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (group == null)
            throw new NotFoundException(_localizer["BusinessGroup_NotFound"]);

        if (!group.IsActive && !isAdmin)
            throw new NotFoundException(_localizer["BusinessGroup_NotFound"]);

        var detail = _mapper.Map<BusinessGroupDetailDto>(group);
        var membership = me != null ? await GetMembershipAsync(id, me.Value) : null;
        var isOwner = me != null && group.CreatedByUserId == me.Value;

        // Hội nhóm chưa được admin duyệt: chỉ admin, chủ hội và thành viên xem được
        if (group.Type == BusinessGroupType.Community
            && group.ApprovalStatus != GroupApprovalStatus.Approved
            && !isAdmin && !isOwner && membership == null)
        {
            throw new NotFoundException(_localizer["BusinessGroup_NotFound"]);
        }

        detail.MyMemberStatus = membership?.Status;
        detail.IsMember = membership?.Status == GroupMemberStatus.Active;
        detail.IsOwner = isOwner;
        detail.CanViewPosts = isAdmin || detail.IsMember;
        detail.IsAdmin = isAdmin;

        var membersQuery = _queryService.GetAllNoTracking<BusinessGroupMember>()
            .Where(x => x.BusinessGroupId == id)
            .Where(x => isAdmin ? x.Status != GroupMemberStatus.Left : x.Status == GroupMemberStatus.Active)
            .OrderBy(x => x.CreatedAt)
            // Họ tên/SĐT/email của thành viên nằm ở bảng Users
            .Include(x => x.User);

        var members = await membersQuery.Take(20).ToListAsync();
        detail.Members = _mapper.Map<List<BusinessGroupMemberResponseDto>>(members);
        for (var i = 0; i < members.Count; i++) FillMemberPersonalInfo(members[i], detail.Members[i]);
        await _referralService.FillNamesAsync(detail.Members, m => m.ReferralCode, (m, name) => m.ReferralName = name);
        await _referralService.FillNamesAsync(detail.Members, m => m.ReferredByCode, (m, name) => m.ReferredByName = name);

        if (isAdmin)
        {
            detail.PendingMembersCount = await _queryService.GetAllNoTracking<BusinessGroupMember>()
                .CountAsync(x => x.BusinessGroupId == id && x.Status == GroupMemberStatus.Pending);
            detail.PrivateRequestsCount = await _queryService.GetAllNoTracking<BusinessGroupPost>()
                .CountAsync(x => x.BusinessGroupId == id && x.IsPrivateToAdmin && !x.IsHidden);
        }

        return detail;
    }

    public async Task<JoinBusinessGroupResponseDto> JoinAsync(Guid id, JoinBusinessGroupRequest request)
    {
        var group = await _repository.GetFirstAsync(g => g.Id == id && !g.IsDeleted)
            ?? throw new NotFoundException(_localizer["BusinessGroup_NotFound"]);

        if (!group.IsActive)
            throw new BusinessException(_localizer["BusinessGroup_NotOpen"]);

        var currentUserId = GetCurrentUserId();
        Guid userId;
        var isNewAccount = false;
        var accountAlreadyExisted = false;
        var isGuestAccount = false;
        string? username = null;

        if (currentUserId != null)
        {
            userId = currentUserId.Value;
        }
        else
        {
            // Khách chưa đăng nhập → bắt buộc Họ tên + SĐT để tạo tài khoản và liên hệ
            if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Phone))
                throw new BusinessException(_localizer["BusinessGroup_ContactRequired"]);

            // Dùng lại tài khoản theo SĐT/email nếu đã có, tạo tài khoản tự động nếu chưa
            // rồi cập nhật thông tin cá nhân vào bảng Users (không ghi đè hồ sơ tài khoản đã có).
            var resolved = await _userService.ResolvePublicUserAsync(request.FullName, request.Phone, request.Email, request.Zalo);
            userId = resolved.UserId;
            isNewAccount = resolved.IsNewAccount;
            accountAlreadyExisted = !resolved.IsNewAccount;
            isGuestAccount = resolved.IsGuestAccount;

            // Người tham gia được lưu ở bảng Collaborators (chờ duyệt) như luồng mua chung
            await _collaboratorService.EnsureProfileForUserAsync(userId);
        }

        var account = await _userService.FindByIdAsync(userId);

        // Tài khoản vừa tạo tự động: trả tên đăng nhập (user<sđt>) cho khách để đăng nhập lại
        if (isNewAccount) username = account?.Username;

        // Mã chia sẻ của link người này dùng để xin vào nhóm: lần đầu thì ghi nhận vào tài khoản,
        // các lần sau lấy mã đã ghi nhận (mã không tồn tại thì bỏ qua).
        var referralCode = await _referralService.ResolveForUserAsync(userId, request.ReferralCode);

        // Thành viên đã có bản ghi trong nhóm → tái kích hoạt thay vì tạo trùng (unique index GroupId+UserId)
        var member = await _memberRepository.GetFirstAsync(m =>
            m.BusinessGroupId == id && m.UserId == userId);

        var memberStatus = group.RequiresApproval ? GroupMemberStatus.Pending : GroupMemberStatus.Active;

        if (member != null)
        {
            if (member.Status == GroupMemberStatus.Active || member.Status == GroupMemberStatus.Pending)
                throw new BusinessException(_localizer["BusinessGroup_AlreadyJoined"]);

            member.Status = memberStatus;
            member.Note = request.Note?.Trim();
            if (referralCode != null) member.ReferralCode = referralCode;
            member.JoinedAt = memberStatus == GroupMemberStatus.Active ? DateTime.UtcNow : null;
            member.RejectionReason = null;
            _memberRepository.Update(member);
        }
        else
        {
            member = new BusinessGroupMember
            {
                BusinessGroupMemberCode = CodeGenerator.Generate("BGM"),
                BusinessGroupId = id,
                UserId = userId,
                Note = request.Note?.Trim(),
                ReferralCode = referralCode,
                Role = GroupMemberRole.Member,
                Status = memberStatus,
                IsGuestAccount = isGuestAccount,
                JoinedAt = memberStatus == GroupMemberStatus.Active ? DateTime.UtcNow : null
            };

            await _memberRepository.AddAsync(member);
        }

        // Người đã đăng nhập: thông tin cá nhân gửi kèm form ghi vào bảng Users (chỉ điền chỗ còn trống)
        if (currentUserId != null)
            await _userService.UpdatePersonalInfoAsync(userId, request.FullName, request.Phone, request.Email, request.Zalo);

        await _memberRepository.SaveChangesAsync();

        // Ghi nhận phát sinh giới thiệu khi xin vào nhóm.
        await _referralService.RecordEventAsync(referralCode, userId, ReferralEventType.GroupMemberJoin,
            member.Id, group.BusinessGroupCode, null, isGuestAccount);

        // Đồng bộ số thành viên đang hoạt động (sau khi đã lưu bản ghi thành viên)
        var activeCount = await _queryService.GetAllNoTracking<BusinessGroupMember>()
            .CountAsync(x => x.BusinessGroupId == id && x.Status == GroupMemberStatus.Active);

        if (group.MembersCount != activeCount)
        {
            group.MembersCount = activeCount;
            _repository.Update(group);
            await _repository.SaveChangesAsync();
        }

        return new JoinBusinessGroupResponseDto
        {
            MemberId = member.Id,
            Status = memberStatus,
            MembersCount = group.MembersCount,
            RequiresApproval = group.RequiresApproval,
            IsGroupActive = group.IsActive,
            Account = currentUserId == null
                ? new AccountCredentialsDto
                {
                    IsNewAccount = isNewAccount,
                    AccountAlreadyExisted = accountAlreadyExisted,
                    Username = username,
                    PasswordIsPhone = isNewAccount
                }
                : null,
            Message = memberStatus == GroupMemberStatus.Active
                ? _localizer["BusinessGroup_JoinActive"]
                : _localizer["BusinessGroup_JoinPending"]
        };
    }

    public async Task LeaveAsync(Guid id)
    {
        var me = GetCurrentUserId() ?? throw new UnauthorizedException(_localizer["UserNotAuthenticated"]);

        var member = await _memberRepository.GetFirstAsync(m =>
            m.BusinessGroupId == id && m.UserId == me);

        if (member == null || member.Status == GroupMemberStatus.Left)
            throw new BusinessException(_localizer["BusinessGroup_NotMember"]);

        var wasActive = member.Status == GroupMemberStatus.Active;
        member.Status = GroupMemberStatus.Left;
        _memberRepository.Update(member);
        await _memberRepository.SaveChangesAsync();

        // Rời nhóm → trừ phát sinh giới thiệu của thành viên này khỏi thống kê.
        await _referralService.SetEventStatusAsync(ReferralEventType.GroupMemberJoin, member.Id, ReferralEventStatus.Cancelled);

        if (wasActive)
        {
            var group = await _repository.GetByIdAsync(id);
            if (group != null)
            {
                group.MembersCount = await _queryService.GetAllNoTracking<BusinessGroupMember>()
                    .CountAsync(x => x.BusinessGroupId == id && x.Status == GroupMemberStatus.Active);
                _repository.Update(group);
                await _repository.SaveChangesAsync();
            }
        }
    }

    // =====================================================================
    // BÀI ĐĂNG + BÌNH LUẬN TRONG NHÓM
    // =====================================================================

    public async Task<PagedList<BusinessGroupPostResponseDto>> GetPostsAsync(Guid groupId, GroupPostQueryDto query)
    {
        var me = GetCurrentUserId();
        var isAdmin = _currentUserService.IsInRole(UserRole.Admin);
        await EnsureCanViewPostsAsync(groupId, me, isAdmin);

        if (query.PrivateOnly && !isAdmin)
            throw new ForbiddenException(_localizer["BusinessGroup_AdminOnly"]);

        var search = query.Search.NormalizeSearchFilter();
        // Từ khoá đã trim + escape; mẫu LIKE được ghép ngay trong biểu thức truy vấn.
        var searchTerm = search?.RemoveVietnameseSign().ToLikeEscaped();

        var q = _queryService.GetAllNoTracking<BusinessGroupPost>()
            .Where(x => x.BusinessGroupId == groupId)
            .WhereIf(!isAdmin, x => !x.IsHidden && !x.IsPrivateToAdmin)
            .WhereIf(query.PrivateOnly, x => x.IsPrivateToAdmin)
            .WhereIf(query.Type.HasValue, x => x.Type == query.Type!.Value)
            .WhereIf(!string.IsNullOrEmpty(search), x =>
                (x.Title != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.Title), "%" + searchTerm + "%", "\\")) ||
                EF.Functions.ILike(KindiDbFunctions.Unaccent(x.Content), "%" + searchTerm + "%", "\\") ||
                (x.RefCode != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.RefCode), "%" + searchTerm + "%", "\\")))
            .Include(x => x.Author)
            .OrderByDescending(x => x.IsPinned)
            .ThenByDescending(x => x.CreatedAt);

        var paged = await q.ToPagedListAsync(query.Page, query.PageSize, null, null, defaultSortBy: "CreatedAt");
        var result = _mapper.MapPagedList<BusinessGroupPost, BusinessGroupPostResponseDto>(paged);
        await _referralService.FillNamesAsync(result.Items, p => p.ReferralCode, (p, name) => p.ReferralName = name);
        return result;
    }

    public async Task<BusinessGroupPostResponseDto> CreatePostAsync(Guid groupId, CreateBusinessGroupPostDto request)
    {
        var me = GetCurrentUserId() ?? throw new UnauthorizedException(_localizer["UserNotAuthenticated"]);
        var isAdmin = _currentUserService.IsInRole(UserRole.Admin);
        await EnsureCanViewPostsAsync(groupId, me, isAdmin);

        var post = new BusinessGroupPost
        {
            BusinessGroupPostCode = CodeGenerator.Generate("GBP"),
            BusinessGroupId = groupId,
            AuthorId = me,
            Content = request.Content.Trim(),
            Type = request.Type,
            RefId = request.RefId,
            RefCode = string.IsNullOrWhiteSpace(request.RefCode) ? null : request.RefCode.Trim(),
            IsPrivateToAdmin = request.IsPrivateToAdmin
        };

        if (isAdmin)
        {
            post.Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim();
            post.IsPinned = false;
        }
        else
        {
            post.Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim();

            // Thành viên chỉ được thảo luận, HOẶC chuyển tiếp yêu cầu mua chung / tìm nhà cung cấp
            // của hệ thống vào nhóm (bắt buộc kèm bản ghi gốc để đối chiếu) — không gửi offer/thông báo của admin
            var isForwardedRequest =
                request.RefId.HasValue &&
                !string.IsNullOrWhiteSpace(request.RefCode) &&
                (request.Type == GroupPostType.GroupBuyingRequest || request.Type == GroupPostType.SupplierRequest);

            post.Type = isForwardedRequest ? request.Type : GroupPostType.Discussion;

            // Bài chuyển tiếp luôn hiển thị cho cả nhóm, không để ở dạng yêu cầu kín cho admin
            if (isForwardedRequest)
            {
                post.IsPrivateToAdmin = false;
            }
        }

        // Bài chuyển tiếp bản ghi gốc: người gửi chính là người chia sẻ
        // → link chia sẻ kèm mã CTV của họ (admin chia sẻ thì lấy mã của admin).
        var isSharedPost = post.RefId.HasValue && !string.IsNullOrWhiteSpace(post.RefCode);
        if (isSharedPost)
        {
            post.ReferralCode = await _referralService.GetSharerReferralCodeAsync();
            post.WithShareLink = request.WithShareLink;
        }

        await _postRepository.AddAsync(post);
        await _postRepository.SaveChangesAsync();

        var group = await _repository.GetByIdAsync(groupId);
        if (group != null)
        {
            group.PostsCount = await _queryService.GetAllNoTracking<BusinessGroupPost>()
                .CountAsync(x => x.BusinessGroupId == groupId);
            _repository.Update(group);
            await _repository.SaveChangesAsync();
        }

        post.Author = (await _userService.FindByIdAsync(me))!;
        return _mapper.Map<BusinessGroupPostResponseDto>(post);
    }

    /// <summary>
    /// Nhóm ngành đã có bài gắn với bản ghi này — UI dùng để cảnh báo "đã gửi" và không chuyển tiếp lại.
    /// </summary>
    public async Task<List<ForwardedGroupResponseDto>> GetForwardedGroupsAsync(Guid refId)
    {
        if (refId == Guid.Empty) return new List<ForwardedGroupResponseDto>();

        var groupIds = await _queryService.GetAllNoTracking<BusinessGroupPost>()
            .Where(x => x.RefId == refId)
            .Select(x => x.BusinessGroupId)
            .Distinct()
            .ToListAsync();

        if (groupIds.Count == 0) return new List<ForwardedGroupResponseDto>();

        // Admin xem mọi nhóm; người dùng chỉ thấy nhóm mình đang tham gia.
        var isAdmin = _currentUserService.IsInRole(UserRole.Admin);
        var me = GetCurrentUserId();
        var myGroupIds = isAdmin || me == null
            ? new List<Guid>()
            : await _queryService.GetAllNoTracking<BusinessGroupMember>()
                .Where(m => m.UserId == me.Value && m.Status == GroupMemberStatus.Active)
                .Select(m => m.BusinessGroupId)
                .ToListAsync();

        if (!isAdmin && myGroupIds.Count == 0) return new List<ForwardedGroupResponseDto>();

        return await _queryService.GetAllNoTracking<BusinessGroup>()
            .Where(x => x.Type == BusinessGroupType.Industry)
            .Contains(x => x.Id, groupIds, _queryOptions)
            .WhereIf(!isAdmin, x => myGroupIds.Contains(x.Id))
            .OrderBy(x => x.Name)
            .Select(x => new ForwardedGroupResponseDto
            {
                GroupId = x.Id,
                GroupCode = x.BusinessGroupCode,
                Name = x.Name
            })
            .ToListAsync();
    }

    public async Task<BusinessGroupPostResponseDto> UpdatePostAsync(Guid groupId, Guid postId, UpdateBusinessGroupPostDto request)
    {
        var post = await _postRepository.GetFirstAsync(p => p.Id == postId && p.BusinessGroupId == groupId && !p.IsDeleted)
            ?? throw new NotFoundException(_localizer["BusinessGroup_PostNotFound"]);

        post.Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim();
        post.Content = request.Content.Trim();
        post.IsPinned = request.IsPinned;
        post.IsHidden = request.IsHidden;
        _postRepository.Update(post);
        await _postRepository.SaveChangesAsync();

        post.Author = (await _userService.FindByIdAsync(post.AuthorId))!;
        return _mapper.Map<BusinessGroupPostResponseDto>(post);
    }

    public async Task DeletePostAsync(Guid groupId, Guid postId)
    {
        var me = GetCurrentUserId() ?? throw new UnauthorizedException(_localizer["UserNotAuthenticated"]);
        var isAdmin = _currentUserService.IsInRole(UserRole.Admin);

        var post = await _postRepository.GetFirstAsync(p => p.Id == postId && p.BusinessGroupId == groupId && !p.IsDeleted)
            ?? throw new NotFoundException(_localizer["BusinessGroup_PostNotFound"]);

        if (!isAdmin && post.AuthorId != me)
            throw new ForbiddenException(_localizer["BusinessGroup_DeletePostForbidden"]);

        post.IsDeleted = true;
        _postRepository.Update(post);
        await _postRepository.SaveChangesAsync();

        var group = await _repository.GetByIdAsync(groupId);
        if (group != null)
        {
            group.PostsCount = await _queryService.GetAllNoTracking<BusinessGroupPost>()
                .CountAsync(x => x.BusinessGroupId == groupId);
            _repository.Update(group);
            await _repository.SaveChangesAsync();
        }
    }

    public async Task<PagedList<BusinessGroupCommentResponseDto>> GetCommentsAsync(Guid postId, GroupCommentQueryDto query)
    {
        var me = GetCurrentUserId();
        var isAdmin = _currentUserService.IsInRole(UserRole.Admin);
        var post = await GetPostOrThrowAsync(postId);
        await EnsureCanViewPostsAsync(post.BusinessGroupId, me, isAdmin);

        var q = _queryService.GetAllNoTracking<BusinessGroupComment>()
            .Where(x => x.BusinessGroupPostId == postId && !x.IsHidden)
            .Include(x => x.User);

        var paged = await q.ToPagedListAsync(query.Page, query.PageSize, null, null, defaultSortBy: "CreatedAt");
        return _mapper.MapPagedList<BusinessGroupComment, BusinessGroupCommentResponseDto>(paged);
    }

    public async Task<BusinessGroupCommentResponseDto> CreateCommentAsync(Guid postId, CreateBusinessGroupCommentDto request)
    {
        var me = GetCurrentUserId() ?? throw new UnauthorizedException(_localizer["UserNotAuthenticated"]);
        var isAdmin = _currentUserService.IsInRole(UserRole.Admin);
        var post = await GetPostOrThrowAsync(postId);
        await EnsureCanViewPostsAsync(post.BusinessGroupId, me, isAdmin);

        var comment = new BusinessGroupComment
        {
            BusinessGroupCommentCode = CodeGenerator.Generate("GBC"),
            BusinessGroupPostId = postId,
            UserId = me,
            Content = request.Content.Trim(),
            ParentCommentId = request.ParentCommentId
        };

        await _commentRepository.AddAsync(comment);
        await _commentRepository.SaveChangesAsync();

        post.CommentsCount = await _queryService.GetAllNoTracking<BusinessGroupComment>()
            .CountAsync(x => x.BusinessGroupPostId == postId && !x.IsHidden);
        _postRepository.Update(post);
        await _postRepository.SaveChangesAsync();

        comment.User = (await _userService.FindByIdAsync(me))!;
        return _mapper.Map<BusinessGroupCommentResponseDto>(comment);
    }

    public async Task DeleteCommentAsync(Guid postId, Guid commentId)
    {
        var me = GetCurrentUserId() ?? throw new UnauthorizedException(_localizer["UserNotAuthenticated"]);
        var isAdmin = _currentUserService.IsInRole(UserRole.Admin);

        var comment = await _commentRepository.GetFirstAsync(c =>
            c.Id == commentId && c.BusinessGroupPostId == postId && !c.IsDeleted)
            ?? throw new NotFoundException(_localizer["BusinessGroup_CommentNotFound"]);

        if (!isAdmin && comment.UserId != me)
            throw new ForbiddenException(_localizer["BusinessGroup_DeleteCommentForbidden"]);

        comment.IsHidden = true;
        _commentRepository.Update(comment);
        await _commentRepository.SaveChangesAsync();

        var post = await GetPostOrThrowAsync(postId);
        post.CommentsCount = await _queryService.GetAllNoTracking<BusinessGroupComment>()
            .CountAsync(x => x.BusinessGroupPostId == postId && !x.IsHidden);
        _postRepository.Update(post);
        await _postRepository.SaveChangesAsync();
    }

    // =====================================================================
    // HỘI NHÓM (NGƯỜI DÙNG TỰ TẠO THEO CHỦ ĐỀ)
    // =====================================================================

    /// <summary>
    /// Danh sách hội nhóm: hội đã được admin duyệt (mọi người xem được để xin vào)
    /// + hội của chính mình ở mọi trạng thái (kèm nhãn chờ duyệt / bị từ chối).
    /// </summary>
    public async Task<PagedList<BusinessGroupResponseDto>> GetCommunityPagedAsync(BusinessGroupQueryDto query)
    {
        var me = GetCurrentUserId();
        var search = query.Search.NormalizeSearchFilter();
        // Từ khoá đã trim + escape; mẫu LIKE được ghép ngay trong biểu thức truy vấn.
        var searchTerm = search?.RemoveVietnameseSign().ToLikeEscaped();

        var q = _queryService.GetAllNoTracking<BusinessGroup>()
            .Where(x => x.Type == BusinessGroupType.Community)
            .WhereIf(!string.IsNullOrEmpty(search), x =>
                EF.Functions.ILike(KindiDbFunctions.Unaccent(x.Name), "%" + searchTerm + "%", "\\") ||
                (x.Topic != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.Topic), "%" + searchTerm + "%", "\\")) ||
                (x.Description != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.Description), "%" + searchTerm + "%", "\\")));

        if (me != null)
        {
            q = query.MineOnly
                // Chỉ hội của tôi (chủ hội hoặc thành viên), mọi trạng thái duyệt
                ? q.Where(x => x.CreatedByUserId == me.Value || x.Members.Any(m => m.UserId == me.Value))
                // Mặc định: hội đã duyệt + hội của tôi
                : q.Where(x => x.ApprovalStatus == GroupApprovalStatus.Approved
                               || x.CreatedByUserId == me.Value
                               || x.Members.Any(m => m.UserId == me.Value));
        }
        else
        {
            q = q.Where(x => x.ApprovalStatus == GroupApprovalStatus.Approved);
        }

        var paged = await q.ToPagedListAsync(query.Page, query.PageSize, null, null, defaultSortBy: "CreatedAt");
        var result = _mapper.MapPagedList<BusinessGroup, BusinessGroupResponseDto>(paged);

        await ApplyViewerStateAsync(result.Items, me, includePendingCounts: false);
        return result;
    }

    /// <summary>
    /// Nhóm của tôi: nhóm do chính mình tạo và/hoặc nhóm mình đã tham gia (cả nhóm ngành lẫn hội nhóm),
    /// gồm mọi trạng thái duyệt để xem lại lịch sử.
    /// </summary>
    public async Task<PagedList<BusinessGroupResponseDto>> GetMinePagedAsync(BusinessGroupQueryDto query)
    {
        var meId = GetCurrentUserId() ?? throw new UnauthorizedException(_localizer["UserNotAuthenticated"]);
        var search = query.Search.NormalizeSearchFilter();
        // Từ khoá đã trim + escape; mẫu LIKE được ghép ngay trong biểu thức truy vấn.
        var searchTerm = search?.RemoveVietnameseSign().ToLikeEscaped();

        var q = _queryService.GetAllNoTracking<BusinessGroup>()
            .WhereIf(!string.IsNullOrEmpty(search), x =>
                EF.Functions.ILike(KindiDbFunctions.Unaccent(x.Name), "%" + searchTerm + "%", "\\") ||
                (x.Topic != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.Topic), "%" + searchTerm + "%", "\\")) ||
                (x.Description != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.Description), "%" + searchTerm + "%", "\\")));

        // Vai trò của mình với nhóm: nhóm mình tạo, nhóm mình đã tham gia, hoặc cả hai
        if (query.MineRole == GroupMineRole.Created)
            q = q.Where(x => x.CreatedByUserId == meId);
        else if (query.MineRole == GroupMineRole.Joined)
            q = q.Where(x => x.Members.Any(m => m.UserId == meId));
        else
            q = q.Where(x => x.CreatedByUserId == meId || x.Members.Any(m => m.UserId == meId));

        var paged = await q.ToPagedListAsync(query.Page, query.PageSize, null, null, defaultSortBy: "CreatedAt");
        var result = _mapper.MapPagedList<BusinessGroup, BusinessGroupResponseDto>(paged);

        // Nhóm mình tạo cần số người chờ duyệt để duyệt thành viên
        await ApplyViewerStateAsync(result.Items, meId, includePendingCounts: true);
        return result;
    }

    /// <summary>
    /// Người dùng tạo hội nhóm theo chủ đề: hội ở trạng thái chờ admin duyệt,
    /// người tạo trở thành chủ hội (quản trị hội) và tự duyệt thành viên.
    /// </summary>
    public async Task<BusinessGroupResponseDto> CreateCommunityAsync(CreateCommunityGroupDto request)
    {
        var me = GetCurrentUserId() ?? throw new UnauthorizedException(_localizer["UserNotAuthenticated"]);

        var group = new BusinessGroup
        {
            BusinessGroupCode = CodeGenerator.Generate("CLB"),
            Name = request.Name.Trim(),
            Topic = request.Topic.Trim(),
            Description = request.Description?.Trim(),
            Type = BusinessGroupType.Community,
            ApprovalStatus = GroupApprovalStatus.Pending,
            RequiresApproval = true,
            IsActive = true,
            CreatedByUserId = me
        };

        await _repository.AddAsync(group);
        await _repository.SaveChangesAsync();

        await _memberRepository.AddAsync(new BusinessGroupMember
        {
            BusinessGroupMemberCode = CodeGenerator.Generate("BGM"),
            BusinessGroupId = group.Id,
            UserId = me,
            Role = GroupMemberRole.GroupAdmin,
            Status = GroupMemberStatus.Active,
            JoinedAt = DateTime.UtcNow
        });
        await _memberRepository.SaveChangesAsync();

        group.MembersCount = 1;
        _repository.Update(group);
        await _repository.SaveChangesAsync();

        var dto = _mapper.Map<BusinessGroupResponseDto>(group);
        dto.IsMember = true;
        dto.IsOwner = true;
        dto.MyMemberStatus = GroupMemberStatus.Active;
        return dto;
    }

    /// <summary>Admin duyệt / từ chối mở hội nhóm.</summary>
    public async Task<BusinessGroupResponseDto> UpdateCommunityApprovalAsync(Guid id, UpdateCommunityGroupApprovalDto request)
    {
        var group = await _repository.GetFirstAsync(g => g.Id == id && !g.IsDeleted)
            ?? throw new NotFoundException(_localizer["BusinessGroup_NotFound"]);

        if (group.Type != BusinessGroupType.Community)
            throw new BusinessException(_localizer["BusinessGroup_NotCommunity"]);

        group.ApprovalStatus = request.ApprovalStatus;
        group.RejectedReason = request.ApprovalStatus == GroupApprovalStatus.Rejected
            ? request.RejectedReason?.Trim()
            : null;
        group.IsActive = request.ApprovalStatus == GroupApprovalStatus.Approved;

        _repository.Update(group);
        await _repository.SaveChangesAsync();

        var dto = _mapper.Map<BusinessGroupResponseDto>(group);
        dto.IsOwner = group.CreatedByUserId == GetCurrentUserId();
        return dto;
    }

    // =====================================================================
    // QUẢN TRỊ
    // =====================================================================

    public async Task<PagedList<BusinessGroupResponseDto>> GetAdminPagedAsync(AdminBusinessGroupQueryDto query)
    {
        var search = query.Search.NormalizeSearchFilter();
        // Từ khoá đã trim + escape; mẫu LIKE được ghép ngay trong biểu thức truy vấn.
        var searchTerm = search?.RemoveVietnameseSign().ToLikeEscaped();

        var q = _queryService.GetAllNoTracking<BusinessGroup>()
            .WhereIf(query.Type.HasValue, x => x.Type == query.Type!.Value)
            .WhereIf(query.ApprovalStatus.HasValue, x => x.ApprovalStatus == query.ApprovalStatus!.Value)
            .WhereIf(query.IsActive.HasValue, x => x.IsActive == query.IsActive!.Value)
            .WhereIf(query.BusinessFieldId.HasValue, x => x.BusinessFieldId == query.BusinessFieldId!.Value)
            .WhereIf(!string.IsNullOrEmpty(search), x =>
                EF.Functions.ILike(KindiDbFunctions.Unaccent(x.Name), "%" + searchTerm + "%", "\\") ||
                (x.BusinessGroupCode != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.BusinessGroupCode), "%" + searchTerm + "%", "\\")) ||
                (x.BusinessFieldName != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.BusinessFieldName), "%" + searchTerm + "%", "\\")))
            .WhereIf(query.HasPendingMembers,
                x => x.Members.Any(m => m.Status == GroupMemberStatus.Pending))
            .WhereIf(query.HasPrivateRequests,
                x => x.Posts.Any(p => p.IsPrivateToAdmin && !p.IsHidden))
            .Include(x => x.BusinessField);

        var paged = await q.ToPagedListAsync(query.Page, query.PageSize, null, null, defaultSortBy: "CreatedAt");
        var result = _mapper.MapPagedList<BusinessGroup, BusinessGroupResponseDto>(paged);

        await ApplyViewerStateAsync(result.Items, GetCurrentUserId(), includePendingCounts: true);
        return result;
    }

    public async Task<BusinessGroupDetailDto> GetAdminByIdAsync(Guid id)
    {
        var group = await _queryService.GetAllNoTracking<BusinessGroup>()
            // Lọc trước rồi mới include
            .Where(x => x.Id == id)
            .Include(x => x.BusinessField)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException(_localizer["BusinessGroup_NotFound"]);

        var detail = _mapper.Map<BusinessGroupDetailDto>(group);
        detail.IsAdmin = true;
        detail.CanViewPosts = true;
        detail.IsMember = false;

        var members = await _queryService.GetAllNoTracking<BusinessGroupMember>()
            .Where(x => x.BusinessGroupId == id && x.Status != GroupMemberStatus.Left)
            .OrderBy(x => x.Status)
            .ThenByDescending(x => x.CreatedAt)
            .Include(x => x.User)
            .Take(50)
            .ToListAsync();

        detail.Members = _mapper.Map<List<BusinessGroupMemberResponseDto>>(members);
        for (var i = 0; i < members.Count; i++) FillMemberPersonalInfo(members[i], detail.Members[i]);
        await _referralService.FillNamesAsync(detail.Members, m => m.ReferralCode, (m, name) => m.ReferralName = name);
        await _referralService.FillNamesAsync(detail.Members, m => m.ReferredByCode, (m, name) => m.ReferredByName = name);
        detail.PendingMembersCount = members.Count(m => m.Status == GroupMemberStatus.Pending);
        detail.PrivateRequestsCount = await _queryService.GetAllNoTracking<BusinessGroupPost>()
            .CountAsync(x => x.BusinessGroupId == id && x.IsPrivateToAdmin && !x.IsHidden);

        return detail;
    }

    public async Task<BusinessGroupResponseDto> CreateAsync(CreateBusinessGroupDto request)
    {
        var group = new BusinessGroup
        {
            BusinessGroupCode = CodeGenerator.Generate("GRP"),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            BusinessFieldId = request.BusinessFieldId,
            BusinessFieldName = request.BusinessFieldName?.Trim(),
            CoverImageUrl = request.CoverImageUrl?.Trim(),
            RequiresApproval = request.RequiresApproval,
            IsActive = request.IsActive,
            CreatedByUserId = GetCurrentUserId()
        };

        await _repository.AddAsync(group);
        await _repository.SaveChangesAsync();

        var dto = _mapper.Map<BusinessGroupResponseDto>(group);
        dto.BusinessFieldName = await ResolveBusinessFieldNameAsync(group);
        return dto;
    }

    public async Task<BusinessGroupResponseDto> UpdateAsync(Guid id, UpdateBusinessGroupDto request)
    {
        var group = await _repository.GetFirstAsync(g => g.Id == id && !g.IsDeleted)
            ?? throw new NotFoundException(_localizer["BusinessGroup_NotFound"]);

        group.Name = request.Name.Trim();
        group.Description = request.Description?.Trim();
        group.BusinessFieldId = request.BusinessFieldId;
        group.BusinessFieldName = request.BusinessFieldName?.Trim();
        group.CoverImageUrl = request.CoverImageUrl?.Trim();
        group.RequiresApproval = request.RequiresApproval;
        group.IsActive = request.IsActive;

        _repository.Update(group);
        await _repository.SaveChangesAsync();

        var dto = _mapper.Map<BusinessGroupResponseDto>(group);
        dto.BusinessFieldName = await ResolveBusinessFieldNameAsync(group);
        return dto;
    }

    public async Task DeleteAsync(Guid id)
    {
        var group = await _repository.GetFirstAsync(g => g.Id == id && !g.IsDeleted)
            ?? throw new NotFoundException(_localizer["BusinessGroup_NotFound"]);

        group.IsDeleted = true;
        _repository.Update(group);
        await _repository.SaveChangesAsync();
    }

    public async Task<PagedList<BusinessGroupMemberResponseDto>> GetMembersAsync(Guid id, BusinessGroupMemberQueryDto query)
    {
        var group = await _repository.GetFirstAsync(g => g.Id == id && !g.IsDeleted)
            ?? throw new NotFoundException(_localizer["BusinessGroup_NotFound"]);

        // Nhóm ngành: admin; Hội nhóm: admin hoặc chủ hội
        EnsureCanManageMembers(group);

        var search = query.Search.NormalizeSearchFilter();
        // Từ khoá đã trim + escape; mẫu LIKE được ghép ngay trong biểu thức truy vấn.
        var searchTerm = search?.RemoveVietnameseSign().ToLikeEscaped();

        var q = _queryService.GetAllNoTracking<BusinessGroupMember>()
            .Where(x => x.BusinessGroupId == id)
            .WhereIf(query.Status.HasValue, x => x.Status == query.Status!.Value)
            // Họ tên/SĐT/email của thành viên nằm ở bảng Users
            .WhereIf(!string.IsNullOrEmpty(search), x =>
                (x.User != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.User.FullName), "%" + searchTerm + "%", "\\")) ||
                (x.User != null && x.User.Phone != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.User.Phone), "%" + searchTerm + "%", "\\")) ||
                (x.User != null && x.User.Email != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.User.Email), "%" + searchTerm + "%", "\\")))
            .Include(x => x.User)
            .OrderBy(x => x.Status)
            .ThenByDescending(x => x.CreatedAt);

        var paged = await q.ToPagedListAsync(query.Page, query.PageSize, null, null, defaultSortBy: "CreatedAt");
        var result = _mapper.MapPagedList<BusinessGroupMember, BusinessGroupMemberResponseDto>(paged);
        for (var i = 0; i < result.Items.Count; i++) FillMemberPersonalInfo(paged.Items[i], result.Items[i]);
        await _referralService.FillNamesAsync(result.Items, m => m.ReferralCode, (m, name) => m.ReferralName = name);
        await _referralService.FillNamesAsync(result.Items, m => m.ReferredByCode, (m, name) => m.ReferredByName = name);
        return result;
    }

    public async Task<BusinessGroupMemberResponseDto> UpdateMemberStatusAsync(Guid id, Guid memberId, UpdateGroupMemberStatusDto request)
    {
        var group = await _repository.GetFirstAsync(g => g.Id == id && !g.IsDeleted)
            ?? throw new NotFoundException(_localizer["BusinessGroup_NotFound"]);

        EnsureCanManageMembers(group);

        var member = await _memberRepository.GetFirstAsync(m =>
            m.Id == memberId && m.BusinessGroupId == id && !m.IsDeleted)
            ?? throw new NotFoundException(_localizer["BusinessGroup_MemberNotFound"]);

        var wasActive = member.Status == GroupMemberStatus.Active;

        member.Status = request.Status;
        member.RejectionReason = request.Status == GroupMemberStatus.Rejected
            ? request.RejectionReason?.Trim()
            : null;
        member.ApprovedAt = request.Status == GroupMemberStatus.Active ? DateTime.UtcNow : null;
        member.ApprovedByUserId = GetCurrentUserId();
        member.JoinedAt = request.Status == GroupMemberStatus.Active
            ? (member.JoinedAt ?? DateTime.UtcNow)
            : member.JoinedAt;

        _memberRepository.Update(member);
        await _memberRepository.SaveChangesAsync();

        // Đồng bộ lại số thành viên đang hoạt động của nhóm
        if (wasActive != (request.Status == GroupMemberStatus.Active))
        {
            group.MembersCount = await _queryService.GetAllNoTracking<BusinessGroupMember>()
                .CountAsync(x => x.BusinessGroupId == id && x.Status == GroupMemberStatus.Active);
            _repository.Update(group);
            await _repository.SaveChangesAsync();
        }

        // Họ tên/SĐT/email của thành viên nằm ở bảng Users
        member.User = (await _userService.FindByIdAsync(member.UserId))!;
        var dto = _mapper.Map<BusinessGroupMemberResponseDto>(member);
        FillMemberPersonalInfo(member, dto);
        await _referralService.FillNamesAsync(new[] { dto }, m => m.ReferralCode, (m, name) => m.ReferralName = name);
        await _referralService.FillNamesAsync(new[] { dto }, m => m.ReferredByCode, (m, name) => m.ReferredByName = name);
        return dto;
    }

    public async Task RemoveMemberAsync(Guid id, Guid memberId)
    {
        var group = await _repository.GetFirstAsync(g => g.Id == id && !g.IsDeleted)
            ?? throw new NotFoundException(_localizer["BusinessGroup_NotFound"]);

        EnsureCanManageMembers(group);

        var member = await _memberRepository.GetFirstAsync(m =>
            m.Id == memberId && m.BusinessGroupId == id && !m.IsDeleted)
            ?? throw new NotFoundException(_localizer["BusinessGroup_MemberNotFound"]);

        var wasActive = member.Status == GroupMemberStatus.Active;
        member.IsDeleted = true;
        member.Status = GroupMemberStatus.Left;
        _memberRepository.Update(member);
        await _memberRepository.SaveChangesAsync();

        // Xoá thành viên khỏi nhóm → trừ phát sinh giới thiệu của thành viên đó.
        await _referralService.SetEventStatusAsync(ReferralEventType.GroupMemberJoin, member.Id, ReferralEventStatus.Cancelled);

        // Đồng bộ số thành viên đang hoạt động (dùng 'group' đã lấy ở đầu hàm)
        if (wasActive)
        {
            group.MembersCount = await _queryService.GetAllNoTracking<BusinessGroupMember>()
                .CountAsync(x => x.BusinessGroupId == id && x.Status == GroupMemberStatus.Active);
            _repository.Update(group);
            await _repository.SaveChangesAsync();
        }
    }

    // =====================================================================
    // HELPERS
    // =====================================================================

    private Guid? GetCurrentUserId()
        => Guid.TryParse(_currentUserService.UserId, out var id) ? id : null;

    /// <summary>
    /// Điền thông tin cá nhân (lấy từ bảng Users) vào DTO thành viên nhóm.
    /// </summary>
    private static void FillMemberPersonalInfo(BusinessGroupMember member, BusinessGroupMemberResponseDto dto)
    {
        dto.FullName = member.User?.FullName ?? string.Empty;
        dto.Phone = member.User?.Phone ?? string.Empty;
        dto.Zalo = member.User?.Zalo;
        dto.Email = UserInfo.DisplayEmail(member.User?.Email, member.User?.Phone);
        // Người giới thiệu thành viên này (ghi nhận trên tài khoản) — chỉ hiển thị ở màn quản trị.
        dto.ReferredByCode = member.User?.ReferredByCode;
    }

    private async Task<BusinessGroupMember?> GetMembershipAsync(Guid groupId, Guid userId)
        => await _queryService.GetAllNoTracking<BusinessGroupMember>()
            .FirstOrDefaultAsync(x => x.BusinessGroupId == groupId && x.UserId == userId);

    private async Task<BusinessGroupPost> GetPostOrThrowAsync(Guid postId)
        => await _postRepository.GetFirstAsync(p => p.Id == postId && !p.IsDeleted)
            ?? throw new NotFoundException(_localizer["BusinessGroup_PostNotFound"]);

    /// <summary>
    /// Quản lý thành viên: admin, hoặc chủ hội nhóm (người tạo hội) tự duyệt thành viên.
    /// </summary>
    private void EnsureCanManageMembers(BusinessGroup group)
    {
        if (_currentUserService.IsInRole(UserRole.Admin)) return;

        var me = GetCurrentUserId();
        if (me != null && group.CreatedByUserId == me.Value) return;

        throw new ForbiddenException(_localizer["BusinessGroup_AdminOnly"]);
    }

    /// <summary>
    /// Chỉ thành viên đã được duyệt (hoặc admin) mới đọc/đăng bài trong nhóm.
    /// </summary>
    private async Task EnsureCanViewPostsAsync(Guid groupId, Guid? userId, bool isAdmin)
    {
        if (isAdmin) return;

        if (userId == null)
            throw new UnauthorizedException(_localizer["UserNotAuthenticated"]);

        var membership = await GetMembershipAsync(groupId, userId.Value);
        if (membership == null || membership.Status != GroupMemberStatus.Active)
            throw new ForbiddenException(_localizer["BusinessGroup_JoinToViewPosts"]);
    }

    private async Task<string?> ResolveBusinessFieldNameAsync(BusinessGroup group)
    {
        if (group.BusinessFieldId == null) return group.BusinessFieldName;

        var field = await _queryService.GetAllNoTracking<BusinessField>()
            .FirstOrDefaultAsync(x => x.Id == group.BusinessFieldId.Value);

        return field?.Name ?? group.BusinessFieldName;
    }

    /// <summary>Gắn trạng thái thành viên của người đang xem + số đếm chờ duyệt (cho admin).</summary>
    private async Task ApplyViewerStateAsync(List<BusinessGroupResponseDto> items, Guid? me, bool includePendingCounts)
    {
        if (items.Count == 0) return;

        var groupIds = items.Select(x => x.Id).ToList();

        if (me != null)
        {
            var memberships = await _queryService.GetAllNoTracking<BusinessGroupMember>()
                .Where(m => m.UserId == me.Value)
                .Contains(m => m.BusinessGroupId, groupIds, _queryOptions)
                .ToListAsync();

            foreach (var item in items)
            {
                var membership = memberships.FirstOrDefault(m => m.BusinessGroupId == item.Id);
                item.MyMemberStatus = membership?.Status;
                item.IsMember = membership?.Status == GroupMemberStatus.Active;
                item.IsOwner = item.CreatedByUserId == me.Value;
            }
        }

        if (includePendingCounts)
        {
            var pending = await _queryService.GetAllNoTracking<BusinessGroupMember>()
                .Where(m => m.Status == GroupMemberStatus.Pending)
                .Contains(m => m.BusinessGroupId, groupIds, _queryOptions)
                .GroupBy(m => m.BusinessGroupId)
                .Select(g => new { GroupId = g.Key, Count = g.Count() })
                .ToListAsync();

            var privateRequests = await _queryService.GetAllNoTracking<BusinessGroupPost>()
                .Where(p => p.IsPrivateToAdmin && !p.IsHidden)
                .Contains(p => p.BusinessGroupId, groupIds, _queryOptions)
                .GroupBy(p => p.BusinessGroupId)
                .Select(g => new { GroupId = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var item in items)
            {
                item.PendingMembersCount = pending.FirstOrDefault(p => p.GroupId == item.Id)?.Count ?? 0;
                item.PrivateRequestsCount = privateRequests.FirstOrDefault(p => p.GroupId == item.Id)?.Count ?? 0;
            }
        }
    }
}
