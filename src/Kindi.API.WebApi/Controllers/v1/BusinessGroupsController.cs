using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.Resources;
using Kindi.API.WebApi.Authorization;
using Kindi.API.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Kindi.API.Shared.Constants;

namespace Kindi.API.WebApi.Controllers.v1;

/// <summary>
/// Nhóm theo lĩnh vực kinh doanh: admin gom nhóm + gửi offer/yêu cầu, thành viên trao đổi.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class BusinessGroupsController : ApiControllerBase
{
    private readonly IBusinessGroupService _groupService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public BusinessGroupsController(
        IBusinessGroupService groupService,
        IStringLocalizer<SharedResource> localizer)
    {
        _groupService = groupService;
        _localizer = localizer;
    }

    // =====================================================================
    // CÔNG KHAI
    // =====================================================================

    /// <summary>Danh sách nhóm đang hoạt động</summary>
    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublic([FromQuery] BusinessGroupQueryDto query)
    {
        var result = await _groupService.GetPublicPagedAsync(query);
        return OkPaged(result, _localizer["BusinessGroup_ListRetrieved"]);
    }

    /// <summary>Chi tiết nhóm (công khai)</summary>
    [HttpGet("{id:guid}/public")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicDetail(Guid id)
    {
        var result = await _groupService.GetPublicByIdAsync(id);
        return Ok(result, _localizer["BusinessGroup_DetailRetrieved"]);
    }

    /// <summary>Xin vào nhóm (khách chưa có tài khoản thì hệ thống tạo tài khoản từ thông tin liên hệ)</summary>
    [HttpPost("{id:guid}/join")]
    [AllowAnonymous]
    public async Task<IActionResult> Join(Guid id, [FromBody] JoinBusinessGroupRequest request)
    {
        var result = await _groupService.JoinAsync(id, request);
        return Ok(result, result.Message);
    }

    /// <summary>Rời nhóm</summary>
    [HttpDelete("{id:guid}/join")]
    [Authorize]
    public async Task<IActionResult> Leave(Guid id)
    {
        await _groupService.LeaveAsync(id);
        return Ok(new { message = _localizer["BusinessGroup_LeaveSuccess"].Value });
    }

    // =====================================================================
    // HỘI NHÓM (người dùng tự tạo theo chủ đề)
    // =====================================================================

    /// <summary>Danh sách hội nhóm: hội đã duyệt + hội của chính mình (mọi trạng thái)</summary>
    [HttpGet("community")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCommunity([FromQuery] BusinessGroupQueryDto query)
    {
        var result = await _groupService.GetCommunityPagedAsync(query);
        return OkPaged(result, _localizer["BusinessGroup_CommunityListRetrieved"]);
    }

    /// <summary>Nhóm của tôi: nhóm mình tạo và/hoặc nhóm mình đã tham gia (nhóm ngành + hội nhóm)</summary>
    [HttpGet("mine")]
    [Authorize]
    public async Task<IActionResult> GetMine([FromQuery] BusinessGroupQueryDto query)
    {
        var result = await _groupService.GetMinePagedAsync(query);
        return OkPaged(result, _localizer["BusinessGroup_ListRetrieved"]);
    }

    /// <summary>Người dùng tạo hội nhóm theo chủ đề (chờ admin duyệt mở hội)</summary>
    [HttpPost("community")]
    [Authorize]
    public async Task<IActionResult> CreateCommunity([FromBody] CreateCommunityGroupDto request)
    {
        var result = await _groupService.CreateCommunityAsync(request);
        return Created(string.Empty, result, _localizer["BusinessGroup_CommunityCreated"]);
    }

    /// <summary>Admin duyệt / từ chối mở hội nhóm</summary>
    [HasPermission(PermissionCode.ApproveCommunityGroup)]
    [HttpPut("community/{id:guid}/approval")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<IActionResult> UpdateCommunityApproval(Guid id, [FromBody] UpdateCommunityGroupApprovalDto request)
    {
        var result = await _groupService.UpdateCommunityApprovalAsync(id, request);
        return Ok(result, _localizer["BusinessGroup_CommunityApproved"]);
    }

    // =====================================================================
    // BÀI ĐĂNG / BÌNH LUẬN TRONG NHÓM (thành viên đã duyệt hoặc admin)
    // =====================================================================

    /// <summary>Danh sách bài trong nhóm</summary>
    /// <summary>
    /// Nhóm ngành đã có bài chuyển tiếp cho bản ghi này — dùng để cảnh báo trước khi gửi.
    /// Admin xem mọi nhóm; người dùng chỉ thấy nhóm mình đang tham gia.
    /// </summary>
    [HttpGet("forwarded-groups")]
    [Authorize]
    public async Task<IActionResult> GetForwardedGroups([FromQuery] Guid refId)
    {
        var data = await _groupService.GetForwardedGroupsAsync(refId);
        return Ok(data, _localizer["BusinessGroup_ForwardedGroupsRetrieved"]);
    }

    [HttpGet("{id:guid}/posts")]
    [Authorize]
    public async Task<IActionResult> GetPosts(Guid id, [FromQuery] GroupPostQueryDto query)
    {
        var result = await _groupService.GetPostsAsync(id, query);
        return OkPaged(result, _localizer["BusinessGroup_PostsRetrieved"]);
    }

    /// <summary>Đăng bài / gửi yêu cầu kín cho admin</summary>
    [HttpPost("{id:guid}/posts")]
    [Authorize]
    public async Task<IActionResult> CreatePost(Guid id, [FromBody] CreateBusinessGroupPostDto request)
    {
        var result = await _groupService.CreatePostAsync(id, request);
        return Ok(result, _localizer["BusinessGroup_PostCreated"]);
    }

    /// <summary>Admin sửa/ghim/ẩn bài trong nhóm</summary>
    [HasPermission(PermissionCode.UpdateGroupPost)]
    [HttpPut("{id:guid}/posts/{postId:guid}")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<IActionResult> UpdatePost(Guid id, Guid postId, [FromBody] UpdateBusinessGroupPostDto request)
    {
        var result = await _groupService.UpdatePostAsync(id, postId, request);
        return Ok(result, _localizer["BusinessGroup_PostUpdated"]);
    }

    /// <summary>Xoá bài (admin hoặc tác giả)</summary>
    [HttpDelete("{id:guid}/posts/{postId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeletePost(Guid id, Guid postId)
    {
        await _groupService.DeletePostAsync(id, postId);
        return Ok(new { message = _localizer["BusinessGroup_PostDeleted"].Value });
    }

    /// <summary>Khôi phục bài trong nhóm đã xoá mềm (admin)</summary>
    [HasPermission(PermissionCode.UpdateGroupPost)]
    [HttpPost("{id:guid}/posts/{postId:guid}/restore")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<IActionResult> RestorePost(Guid id, Guid postId)
    {
        var result = await _groupService.RestorePostAsync(id, postId);
        return Ok(result, _localizer["RestoreSuccess"]);
    }

    /// <summary>Bình luận của bài</summary>
    [HttpGet("posts/{postId:guid}/comments")]
    [Authorize]
    public async Task<IActionResult> GetComments(Guid postId, [FromQuery] GroupCommentQueryDto query)
    {
        var result = await _groupService.GetCommentsAsync(postId, query);
        return OkPaged(result, _localizer["BusinessGroup_CommentsRetrieved"]);
    }

    /// <summary>Thêm bình luận</summary>
    [HttpPost("posts/{postId:guid}/comments")]
    [Authorize]
    public async Task<IActionResult> CreateComment(Guid postId, [FromBody] CreateBusinessGroupCommentDto request)
    {
        var result = await _groupService.CreateCommentAsync(postId, request);
        return Ok(result, _localizer["BusinessGroup_CommentCreated"]);
    }

    /// <summary>Xoá bình luận (admin hoặc tác giả)</summary>
    [HttpDelete("posts/{postId:guid}/comments/{commentId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteComment(Guid postId, Guid commentId)
    {
        await _groupService.DeleteCommentAsync(postId, commentId);
        return Ok(new { message = _localizer["BusinessGroup_CommentDeleted"].Value });
    }

    // =====================================================================
    // QUẢN TRỊ
    // =====================================================================

    /// <summary>Danh sách nhóm (admin)</summary>
    [HasPermission(PermissionCode.ViewGroups)]
    [HttpGet]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<IActionResult> GetList([FromQuery] AdminBusinessGroupQueryDto query)
    {
        var result = await _groupService.GetAdminPagedAsync(query);
        return OkPaged(result, _localizer["BusinessGroup_ListRetrieved"]);
    }

    /// <summary>Chi tiết nhóm (admin — kèm thành viên chờ duyệt)</summary>
    [HasPermission(PermissionCode.ViewGroups)]
    [HttpGet("{id:guid}")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<IActionResult> GetDetail(Guid id)
    {
        var result = await _groupService.GetAdminByIdAsync(id);
        return Ok(result, _localizer["BusinessGroup_DetailRetrieved"]);
    }

    /// <summary>Tạo nhóm</summary>
    [HasPermission(PermissionCode.ManageGroups)]
    [HttpPost]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<IActionResult> Create([FromBody] CreateBusinessGroupDto request)
    {
        var result = await _groupService.CreateAsync(request);
        return Created(string.Empty, result, _localizer["BusinessGroup_Created"]);
    }

    /// <summary>Cập nhật nhóm</summary>
    [HasPermission(PermissionCode.ManageGroups)]
    [HttpPut("{id:guid}")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBusinessGroupDto request)
    {
        var result = await _groupService.UpdateAsync(id, request);
        return Ok(result, _localizer["BusinessGroup_Updated"]);
    }

    /// <summary>Xoá nhóm</summary>
    [HasPermission(PermissionCode.ManageGroups)]
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _groupService.DeleteAsync(id);
        return Ok(new { message = _localizer["BusinessGroup_Deleted"].Value });
    }

    /// <summary>Khôi phục nhóm đã xoá mềm</summary>
    [HasPermission(PermissionCode.ManageGroups)]
    [HttpPost("{id:guid}/restore")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<IActionResult> Restore(Guid id)
    {
        var result = await _groupService.RestoreAsync(id);
        return Ok(result, _localizer["RestoreSuccess"]);
    }

    /// <summary>Danh sách thành viên + yêu cầu vào nhóm (admin hoặc chủ hội nhóm)</summary>
    [HttpGet("{id:guid}/members")]
    [Authorize]
    public async Task<IActionResult> GetMembers(Guid id, [FromQuery] BusinessGroupMemberQueryDto query)
    {
        var result = await _groupService.GetMembersAsync(id, query);
        return OkPaged(result, _localizer["BusinessGroup_MembersRetrieved"]);
    }

    /// <summary>Duyệt / từ chối yêu cầu vào nhóm (admin duyệt nhóm ngành, chủ hội duyệt thành viên hội)</summary>
    [HttpPut("{id:guid}/members/{memberId:guid}/status")]
    [Authorize]
    public async Task<IActionResult> UpdateMemberStatus(Guid id, Guid memberId, [FromBody] UpdateGroupMemberStatusDto request)
    {
        var result = await _groupService.UpdateMemberStatusAsync(id, memberId, request);
        return Ok(result, _localizer["BusinessGroup_MemberStatusUpdated"]);
    }

    /// <summary>Xoá thành viên khỏi nhóm (admin hoặc chủ hội nhóm)</summary>
    [HttpDelete("{id:guid}/members/{memberId:guid}")]
    [Authorize]
    public async Task<IActionResult> RemoveMember(Guid id, Guid memberId)
    {
        await _groupService.RemoveMemberAsync(id, memberId);
        return Ok(new { message = _localizer["BusinessGroup_MemberRemoved"].Value });
    }

    /// <summary>Yêu cầu kín gửi admin trong nhóm</summary>
    [HasPermission(PermissionCode.ViewGroupPrivateRequests)]
    [HttpGet("{id:guid}/private-requests")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<IActionResult> GetPrivateRequests(Guid id, [FromQuery] GroupPostQueryDto query)
    {
        query.PrivateOnly = true;
        var result = await _groupService.GetPostsAsync(id, query);
        return OkPaged(result, _localizer["BusinessGroup_PostsRetrieved"]);
    }
}
