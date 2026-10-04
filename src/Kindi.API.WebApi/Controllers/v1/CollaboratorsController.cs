using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.Requests;
using Kindi.API.Application.Resources;
using Kindi.API.Domain.Enums;
using Kindi.API.WebApi.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Kindi.API.Shared.Constants;

namespace Kindi.API.WebApi.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class CollaboratorsController : ApiControllerBase
{
    private readonly ICollaboratorService _collaboratorService;
    private readonly IReferralService _referralService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CollaboratorsController(
        ICollaboratorService collaboratorService,
        IReferralService referralService,
        IStringLocalizer<SharedResource> localizer)
    {
        _collaboratorService = collaboratorService;
        _referralService = referralService;
        _localizer = localizer;
    }

    /// <summary>
    /// Mã CTV của tài khoản đang đăng nhập — dùng để gắn vào link chia sẻ.
    /// </summary>
    [HttpGet("me/referral-code")]
    public async Task<IActionResult> GetMyReferralCode()
    {
        var referralCode = await _referralService.GetSharerReferralCodeAsync();
        return Ok(new { referralCode });
    }

    /// <summary>
    /// Đăng ký CTV mới
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Create([FromBody] CreateCollaboratorDto request)
    {
        var result = await _collaboratorService.CreateAsync(request);
        return Ok(result, _localizer["Collaborator_CreateSuccess"]);
    }

    /// <summary>
    /// Lấy thông tin CTV theo Id
    /// </summary>
    [Authorize(Roles = RoleConstants.Admin)]
    [HasPermission(PermissionCode.ViewCollaborators)]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _collaboratorService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách CTV phân trang
    /// </summary>
    [Authorize(Roles = RoleConstants.Admin)]
    [HasPermission(PermissionCode.ViewCollaborators)]
    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] string? search = null,
        [FromQuery] CollaboratorStatus? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var result = await _collaboratorService.GetPagedAsync(page, size, search, status, fromDate, toDate);
        return OkPaged(result);
    }

    /// <summary>
    /// Danh sách cộng tác viên đã xóa mềm (Admin)
    /// </summary>
    [HasPermission(PermissionCode.ViewRestoreCollaborator, PermissionCode.RestoreCollaborator)]
    [HttpGet("deleted")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<IActionResult> GetDeleted(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] string? search = null)
    {
        var result = await _collaboratorService.GetPagedDeletedAsync(page, size, search);
        return OkPaged(result);
    }

    /// <summary>
    /// Cập nhật thông tin CTV
    /// </summary>
    [Authorize(Roles = RoleConstants.Admin)]
    [HasPermission(PermissionCode.ViewCollaborators)]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCollaboratorDto request)
    {
        var result = await _collaboratorService.UpdateAsync(id, request);
        return Ok(result, _localizer["Collaborator_UpdateSuccess"]);
    }

    /// <summary>
    /// Duyệt CTV
    /// </summary>
    [HasPermission(PermissionCode.ApproveCollaborator)]
    [HttpPost("{id}/approve")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<IActionResult> Approve(Guid id)
    {
        await _collaboratorService.ApproveAsync(id);
        return Ok(new { message = _localizer["Collaborator_ApproveSuccess"] });
    }

    /// <summary>
    /// Từ chối CTV
    /// </summary>
    [HasPermission(PermissionCode.RejectCollaborator)]
    [HttpPost("{id}/reject")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] string? reason = null)
    {
        await _collaboratorService.RejectAsync(id, reason);
        return Ok(new { message = _localizer["Collaborator_RejectSuccess"] });
    }

    /// <summary>
    /// Xóa mềm CTV
    /// </summary>
    [HasPermission(PermissionCode.DeleteCollaborator)]
    [HttpDelete("{id}")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _collaboratorService.DeleteAsync(id);
        return Ok(new { message = _localizer["Collaborator_DeleteSuccess"] });
    }

    /// <summary>
    /// Khôi phục CTV đã xóa
    /// </summary>
    [HasPermission(PermissionCode.ViewRestoreCollaborator, PermissionCode.RestoreCollaborator)]
    [HttpPost("{id}/restore")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<IActionResult> Restore(Guid id)
    {
        await _collaboratorService.RestoreAsync(id);
        return Ok(new { message = _localizer["Collaborator_RestoreSuccess"] });
    }
}