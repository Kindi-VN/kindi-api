using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.Resources;
using Kindi.API.Domain.Enums;
using Kindi.API.WebApi.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Kindi.API.Shared.Constants;

namespace Kindi.API.WebApi.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class GroupBuyingRequestsController : ApiControllerBase
{
    private readonly IGroupBuyingRequestService _service;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public GroupBuyingRequestsController(
        IGroupBuyingRequestService service,
        IStringLocalizer<SharedResource> localizer)
    {
        _service = service;
        _localizer = localizer;
    }

    /// <summary>
    /// Tạo yêu cầu mua chung mới (khách chưa đăng nhập vẫn tạo được — hệ thống tự tạo User).
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> CreateAsync([FromBody] CreateGroupBuyingRequestDto request)
    {
        var response = await _service.CreateAsync(request);
        return Ok(response, _localizer["GroupBuyingRequest_CreateSuccess"]);
    }

    /// <summary>
    /// Danh sách mua chung cho tab "Mua chung" trên trang social:
    /// nhóm đã duyệt + nhóm do chính người gọi mở (kể cả đang chờ duyệt).
    /// </summary>
    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicList([FromQuery] GetPublicGroupBuyingRequestsQueryDto query)
    {
        var result = await _service.GetPublicPagedAsync(query);
        return OkPaged(result, _localizer["GroupBuyingRequest_PublicRetrievedSuccess"]);
    }

    /// <summary>
    /// Chi tiết mua chung cho người dùng (thông tin liên hệ chỉ hiển thị đầy đủ với admin,
    /// người dùng khác chỉ thấy liên hệ của chính mình).
    /// </summary>
    [HttpGet("{id:guid}/public")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicDetail(Guid id)
    {
        var result = await _service.GetPublicDetailAsync(id);
        return Ok(result, _localizer["GroupBuyingRequest_DetailRetrievedSuccess"]);
    }

    /// <summary>
    /// Chi tiết mua chung theo mã đơn — dùng cho link chia sẻ (người nhận không cần biết Guid).
    /// </summary>
    [HttpGet("code/{code}/public")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicDetailByCode(string code)
    {
        var result = await _service.GetPublicDetailByCodeAsync(code);
        return Ok(result, _localizer["GroupBuyingRequest_DetailRetrievedSuccess"]);
    }

    /// <summary>
    /// Đăng ký tham gia nhóm mua chung. Khách chưa đăng nhập gửi kèm họ tên/SĐT/Zalo/email —
    /// hệ thống tạo tài khoản (username user&lt;sđt&gt;, mật khẩu = sđt) và lưu vào Collaborators.
    /// </summary>
    [HttpPost("{id:guid}/join")]
    [AllowAnonymous]
    public async Task<IActionResult> Join(Guid id, [FromBody] JoinGroupBuyingRequestDto request)
    {
        var result = await _service.JoinAsync(id, request);
        return Ok(result, result.Message);
    }

    /// <summary>
    /// Hủy tham gia nhóm mua chung (người đang đăng nhập).
    /// </summary>
    [HttpDelete("{id:guid}/join")]
    [Authorize]
    public async Task<IActionResult> Leave(Guid id)
    {
        var result = await _service.LeaveAsync(id);
        return Ok(result, _localizer["GroupBuyingRequest_LeaveSuccess"]);
    }

    // ===================== ADMIN =====================

    [Authorize(Roles = RoleConstants.Admin)]
    [HasPermission(PermissionCode.ViewGroupBuyingRequests)]
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] GetGroupBuyingRequestsQueryDto query)
    {
        // Client có thể gửi status rỗng hoặc chuỗi rác ("undefined"/"null") khi không lọc —
        // coi như KHÔNG lọc thay vì trả 400 làm hỏng cả danh sách.
        var result = await _service.GetPagedAsync(query);
        return OkPaged(result, _localizer["GroupBuyingRequest_ListRetrievedSuccess"]);
    }

    [Authorize(Roles = RoleConstants.Admin)]
    [HasPermission(PermissionCode.ViewGroupBuyingRequests)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDetail(Guid id)
    {
        var result = await _service.GetDetailAsync(id);
        return Ok(result, _localizer["GroupBuyingRequest_DetailRetrievedSuccess"]);
    }

    /// <summary>
    /// Duyệt / đóng / hủy yêu cầu mua chung.
    /// </summary>
    [Authorize(Roles = RoleConstants.Admin)]
    [HasPermission(PermissionCode.UpdateGroupBuyingRequestStatus)]
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateGroupBuyingStatusDto request)
    {
        var result = await _service.UpdateStatusAsync(id, request);
        return Ok(result, _localizer["GroupBuyingRequest_StatusUpdatedSuccess"]);
    }

    /// <summary>
    /// Sửa thông tin yêu cầu mua chung (admin).
    /// </summary>
    [Authorize(Roles = RoleConstants.Admin)]
    [HasPermission(PermissionCode.UpdateGroupBuyingRequest)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateGroupBuyingRequestDto request)
    {
        var result = await _service.UpdateAsync(id, request);
        return Ok(result, _localizer["GroupBuyingRequest_UpdatedSuccess"]);
    }

    /// <summary>
    /// Xóa một người khỏi nhóm mua chung (không xóa được người mở nhóm).
    /// </summary>
    [Authorize(Roles = RoleConstants.Admin)]
    [HasPermission(PermissionCode.UpdateGroupBuyingRequest)]
    [HttpDelete("{id:guid}/participants/{participantId:guid}")]
    public async Task<IActionResult> RemoveParticipant(Guid id, Guid participantId)
    {
        var result = await _service.RemoveParticipantAsync(id, participantId);
        return Ok(result, _localizer["GroupBuyingRequest_ParticipantRemovedSuccess"]);
    }

    /// <summary>
    /// Hủy yêu cầu mua chung (xóa mềm).
    /// </summary>
    [Authorize(Roles = RoleConstants.Admin)]
    [HasPermission(PermissionCode.UpdateGroupBuyingRequest)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return Ok(new { message = _localizer["GroupBuyingRequest_DeletedSuccess"] });
    }

    /// <summary>
    /// Khôi phục yêu cầu mua chung đã xoá mềm.
    /// </summary>
    [Authorize(Roles = RoleConstants.Admin)]
    [HasPermission(PermissionCode.UpdateGroupBuyingRequest)]
    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id)
    {
        var result = await _service.RestoreAsync(id);
        return Ok(result, _localizer["RestoreSuccess"]);
    }
}
