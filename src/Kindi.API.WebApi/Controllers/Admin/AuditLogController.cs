using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Application.Resources;
using Kindi.API.Domain.Enums;
using Kindi.API.WebApi.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Kindi.API.Shared.Constants;

namespace Kindi.API.WebApi.Controllers.Admin;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/audit-logs")]
[Authorize(Roles = RoleConstants.Admin)]
[ApiController]
public class AuditLogController : ApiControllerBase
{
    private readonly IAuditLogQueryService _auditLogQueryService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public AuditLogController(
        IAuditLogQueryService auditLogQueryService,
        IStringLocalizer<SharedResource> localizer)
    {
        _auditLogQueryService = auditLogQueryService;
        _localizer = localizer;
    }

    /// <summary>
    /// Danh sách audit log thay đổi entity (Create/Update/Delete).
    /// Không trả về hành động của tài khoản SuperAdmin.
    /// </summary>
    [HttpGet("entity")]
    [HasPermission(PermissionCode.ViewAuthAuditLogs, PermissionCode.ViewEntityAuditLogs)]
    public async Task<IActionResult> GetEntityLogs([FromQuery] AuditLogQueryDto query)
    {
        var result = await _auditLogQueryService.GetEntityLogsAsync(query);
        return OkPaged(result, _localizer["AuditLogs_EntityListSuccess"]);
    }

    /// <summary>
    /// Chi tiết audit log entity theo Id.
    /// </summary>
    [HttpGet("entity/{id:guid}")]
    [HasPermission(PermissionCode.ViewEntityAuditLogs)]
    public async Task<IActionResult> GetEntityLogById(Guid id)
    {
        var result = await _auditLogQueryService.GetEntityLogByIdAsync(id);
        if (result == null)
            return NotFound(_localizer["AuditLogs_NotFound"]);

        return Ok(result, _localizer["AuditLogs_EntityDetailSuccess"]);
    }

    /// <summary>
    /// Danh sách audit log thao tác đăng nhập/đăng ký.
    /// Không trả về hành động của tài khoản SuperAdmin.
    /// </summary>
    [HttpGet("auth")]
    [HasPermission(PermissionCode.ViewAuthAuditLogs)]
    public async Task<IActionResult> GetAuthLogs([FromQuery] AuthAuditLogQueryDto query)
    {
        var result = await _auditLogQueryService.GetAuthLogsAsync(query);
        return OkPaged(result, _localizer["AuditLogs_AuthListSuccess"]);
    }

    /// <summary>
    /// Chi tiết audit log auth theo Id.
    /// </summary>
    [HttpGet("auth/{id:guid}")]
    [HasPermission(PermissionCode.ViewAuthAuditLogs)]
    public async Task<IActionResult> GetAuthLogById(Guid id)
    {
        var result = await _auditLogQueryService.GetAuthLogByIdAsync(id);
        if (result == null)
            return NotFound(_localizer["AuditLogs_NotFound"]);

        return Ok(result, _localizer["AuditLogs_AuthDetailSuccess"]);
    }

    /// <summary>
    /// Toàn bộ audit log thay đổi entity — bao gồm cả hành động của tài khoản SuperAdmin.
    /// </summary>
    [HttpGet("full/entity")]
    [HasPermission(PermissionCode.ViewFullAuditLogs)]
    public async Task<IActionResult> GetFullEntityLogs([FromQuery] AuditLogQueryDto query)
    {
        var result = await _auditLogQueryService.GetEntityLogsAsync(query, includeSuperAdminActors: true);
        return OkPaged(result, _localizer["AuditLogs_EntityListSuccess"]);
    }

    /// <summary>
    /// Chi tiết một dòng audit log entity trong chế độ xem toàn bộ.
    /// </summary>
    [HttpGet("full/entity/{id:guid}")]
    [HasPermission(PermissionCode.ViewFullAuditLogs)]
    public async Task<IActionResult> GetFullEntityLogById(Guid id)
    {
        var result = await _auditLogQueryService.GetEntityLogByIdAsync(id, includeSuperAdminActors: true);
        if (result == null)
            return NotFound(_localizer["AuditLogs_NotFound"]);

        return Ok(result, _localizer["AuditLogs_EntityDetailSuccess"]);
    }

    /// <summary>
    /// Toàn bộ audit log đăng nhập/đăng ký — bao gồm cả hành động của tài khoản SuperAdmin.
    /// </summary>
    [HttpGet("full/auth")]
    [HasPermission(PermissionCode.ViewFullAuditLogs)]
    public async Task<IActionResult> GetFullAuthLogs([FromQuery] AuthAuditLogQueryDto query)
    {
        var result = await _auditLogQueryService.GetAuthLogsAsync(query, includeSuperAdminActors: true);
        return OkPaged(result, _localizer["AuditLogs_AuthListSuccess"]);
    }

    /// <summary>
    /// Chi tiết một dòng audit log đăng nhập/đăng ký trong chế độ xem toàn bộ.
    /// </summary>
    [HttpGet("full/auth/{id:guid}")]
    [HasPermission(PermissionCode.ViewFullAuditLogs)]
    public async Task<IActionResult> GetFullAuthLogById(Guid id)
    {
        var result = await _auditLogQueryService.GetAuthLogByIdAsync(id, includeSuperAdminActors: true);
        if (result == null)
            return NotFound(_localizer["AuditLogs_NotFound"]);

        return Ok(result, _localizer["AuditLogs_AuthDetailSuccess"]);
    }

    /// <summary>
    /// Danh mục chọn nhanh cho bộ lọc nhật ký: hành động của từng loại nhật ký và tên bảng đã phát sinh
    /// nhật ký (lấy từ dữ liệu).
    /// </summary>
    [HttpGet("filters")]
    [HasPermission(PermissionCode.ViewAuthAuditLogs, PermissionCode.ViewEntityAuditLogs)]
    public async Task<IActionResult> GetFilterOptions()
    {
        var result = await _auditLogQueryService.GetFilterOptionsAsync();
        return Ok(result, _localizer["AuditLogs_FilterOptionsSuccess"]);
    }

    /// <summary>
    /// Xoá nhật ký thao tác dữ liệu: theo các dòng được chọn (ids) hoặc theo khoảng ngày
    /// (fromDate/toDate). Bảng nhật ký không có xoá mềm nên đây là xoá VĨNH VIỄN; chính thao tác xoá
    /// được ghi lại một dòng nhật ký để giữ vết.
    /// </summary>
    [HttpDelete("entity")]
    [HasPermission(PermissionCode.DeleteEntityAuditLogs)]
    public async Task<IActionResult> DeleteEntityLogs([FromBody] AuditLogDeleteRequest request)
    {
        var deleted = await _auditLogQueryService.DeleteEntityLogsAsync(request);
        return Ok(new AuditLogDeleteResponse(deleted), _localizer["AuditLogs_DeleteSuccess", deleted]);
    }

    /// <summary>Xoá nhật ký xác thực tài khoản (đăng nhập/đăng ký/đổi mật khẩu...) — xoá VĨNH VIỄN.</summary>
    [HttpDelete("auth")]
    [HasPermission(PermissionCode.DeleteAuthAuditLogs)]
    public async Task<IActionResult> DeleteAuthLogs([FromBody] AuditLogDeleteRequest request)
    {
        var deleted = await _auditLogQueryService.DeleteAuthLogsAsync(request);
        return Ok(new AuditLogDeleteResponse(deleted), _localizer["AuditLogs_DeleteSuccess", deleted]);
    }

    /// <summary>
    /// Xoá nhật ký thao tác dữ liệu trong chế độ xem toàn bộ (kể cả hành động của SuperAdmin).
    /// </summary>
    [HttpDelete("full/entity")]
    [HasPermission(PermissionCode.ViewFullAuditLogs)]
    public async Task<IActionResult> DeleteFullEntityLogs([FromBody] AuditLogDeleteRequest request)
    {
        var deleted = await _auditLogQueryService.DeleteEntityLogsAsync(request, includeSuperAdminActors: true);
        return Ok(new AuditLogDeleteResponse(deleted), _localizer["AuditLogs_DeleteSuccess", deleted]);
    }

    /// <summary>Xoá nhật ký xác thực trong chế độ xem toàn bộ (kể cả hành động của SuperAdmin).</summary>
    [HttpDelete("full/auth")]
    [HasPermission(PermissionCode.ViewFullAuditLogs)]
    public async Task<IActionResult> DeleteFullAuthLogs([FromBody] AuditLogDeleteRequest request)
    {
        var deleted = await _auditLogQueryService.DeleteAuthLogsAsync(request, includeSuperAdminActors: true);
        return Ok(new AuditLogDeleteResponse(deleted), _localizer["AuditLogs_DeleteSuccess", deleted]);
    }
}
