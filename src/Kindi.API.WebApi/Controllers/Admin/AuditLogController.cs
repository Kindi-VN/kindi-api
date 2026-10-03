using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.Resources;
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
    /// Danh sách audit log thay đổi entity (Create/Update/Delete)
    /// </summary>
    [HttpGet("entity")]
    public async Task<IActionResult> GetEntityLogs([FromQuery] AuditLogQueryDto query)
    {
        var result = await _auditLogQueryService.GetEntityLogsAsync(query);
        return OkPaged(result, _localizer["AuditLogs_EntityListSuccess"]);
    }

    /// <summary>
    /// Chi tiết audit log entity theo Id
    /// </summary>
    [HttpGet("entity/{id:guid}")]
    public async Task<IActionResult> GetEntityLogById(Guid id)
    {
        var result = await _auditLogQueryService.GetEntityLogByIdAsync(id);
        if (result == null)
            return NotFound(_localizer["AuditLogs_NotFound"]);

        return Ok(result, _localizer["AuditLogs_EntityDetailSuccess"]);
    }

    /// <summary>
    /// Danh sách audit log thao tác đăng nhập/đăng ký
    /// </summary>
    [HttpGet("auth")]
    public async Task<IActionResult> GetAuthLogs([FromQuery] AuthAuditLogQueryDto query)
    {
        var result = await _auditLogQueryService.GetAuthLogsAsync(query);
        return OkPaged(result, _localizer["AuditLogs_AuthListSuccess"]);
    }

    /// <summary>
    /// Chi tiết audit log auth theo Id
    /// </summary>
    [HttpGet("auth/{id:guid}")]
    public async Task<IActionResult> GetAuthLogById(Guid id)
    {
        var result = await _auditLogQueryService.GetAuthLogByIdAsync(id);
        if (result == null)
            return NotFound(_localizer["AuditLogs_NotFound"]);

        return Ok(result, _localizer["AuditLogs_AuthDetailSuccess"]);
    }
}