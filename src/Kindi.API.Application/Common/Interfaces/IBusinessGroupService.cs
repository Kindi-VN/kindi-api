using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Domain.Models;

namespace Kindi.API.Application.Common.Interfaces;

public interface IBusinessGroupService
{
    // ===== Công khai / thành viên =====
    Task<PagedList<BusinessGroupResponseDto>> GetPublicPagedAsync(BusinessGroupQueryDto query);
    Task<BusinessGroupDetailDto> GetPublicByIdAsync(Guid id);
    Task<JoinBusinessGroupResponseDto> JoinAsync(Guid id, JoinBusinessGroupRequest request);

    // ===== Hội nhóm (người dùng tự tạo) =====
    Task<PagedList<BusinessGroupResponseDto>> GetCommunityPagedAsync(BusinessGroupQueryDto query);

    /// <summary>Nhóm của tôi: nhóm mình tạo và/hoặc nhóm mình đã tham gia (nhóm ngành + hội nhóm)</summary>
    Task<PagedList<BusinessGroupResponseDto>> GetMinePagedAsync(BusinessGroupQueryDto query);
    Task<BusinessGroupResponseDto> CreateCommunityAsync(CreateCommunityGroupDto request);
    Task<BusinessGroupResponseDto> UpdateCommunityApprovalAsync(Guid id, UpdateCommunityGroupApprovalDto request);
    Task LeaveAsync(Guid id);

    Task<PagedList<BusinessGroupPostResponseDto>> GetPostsAsync(Guid groupId, GroupPostQueryDto query);
    Task<BusinessGroupPostResponseDto> CreatePostAsync(Guid groupId, CreateBusinessGroupPostDto request);
    Task DeletePostAsync(Guid groupId, Guid postId);

    /// <summary>Admin khôi phục một bài đăng trong nhóm đã xoá mềm.</summary>
    Task<BusinessGroupPostResponseDto> RestorePostAsync(Guid groupId, Guid postId);

    Task<PagedList<BusinessGroupCommentResponseDto>> GetCommentsAsync(Guid postId, GroupCommentQueryDto query);
    Task<BusinessGroupCommentResponseDto> CreateCommentAsync(Guid postId, CreateBusinessGroupCommentDto request);
    Task DeleteCommentAsync(Guid postId, Guid commentId);

    // ===== Quản trị =====
    Task<PagedList<BusinessGroupResponseDto>> GetAdminPagedAsync(AdminBusinessGroupQueryDto query);
    Task<BusinessGroupDetailDto> GetAdminByIdAsync(Guid id);
    Task<BusinessGroupResponseDto> CreateAsync(CreateBusinessGroupDto request);
    Task<BusinessGroupResponseDto> UpdateAsync(Guid id, UpdateBusinessGroupDto request);
    Task DeleteAsync(Guid id);

    /// <summary>Admin khôi phục một nhóm đã xoá mềm.</summary>
    Task<BusinessGroupResponseDto> RestoreAsync(Guid id);

    Task<PagedList<BusinessGroupMemberResponseDto>> GetMembersAsync(Guid id, BusinessGroupMemberQueryDto query);
    Task<BusinessGroupMemberResponseDto> UpdateMemberStatusAsync(Guid id, Guid memberId, UpdateGroupMemberStatusDto request);
    Task RemoveMemberAsync(Guid id, Guid memberId);
    Task<BusinessGroupPostResponseDto> UpdatePostAsync(Guid groupId, Guid postId, UpdateBusinessGroupPostDto request);

    /// <summary>Danh sách nhóm ngành đã có bài chuyển tiếp cho bản ghi này (để không gửi trùng).</summary>
    Task<List<ForwardedGroupResponseDto>> GetForwardedGroupsAsync(Guid refId);
}
