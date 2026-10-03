using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.Resources;
using Kindi.API.Domain.Enums;
using Kindi.API.WebApi.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Kindi.API.WebApi.Controllers.v1;

/// <summary>
/// Cấu hình mức hoa hồng cho người giới thiệu và đối tác: bản chung cho mọi tài khoản
/// hoặc bản riêng cho một/nhiều tài khoản được chọn.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class CommissionsController : ApiControllerBase
{
    private readonly ICommissionConfigService _commissionConfigService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CommissionsController(
        ICommissionConfigService commissionConfigService,
        IStringLocalizer<SharedResource> localizer)
    {
        _commissionConfigService = commissionConfigService;
        _localizer = localizer;
    }

    /// <summary>Danh sách cấu hình hoa hồng đang áp dụng (bản chung và bản riêng theo tài khoản).</summary>
    [HttpGet]
    [HasPermission(PermissionCode.ViewCommissionConfigs)]
    public async Task<IActionResult> GetPaged([FromQuery] CommissionConfigQueryDto query)
    {
        var result = await _commissionConfigService.GetPagedAsync(query);
        return OkPaged(result, _localizer["Commission_ListSuccess"]);
    }

    /// <summary>Lưu cấu hình hoa hồng cho bản chung hoặc cho một/nhiều tài khoản được chọn.</summary>
    [HttpPost]
    [HasPermission(PermissionCode.UpdateCommissionConfigs)]
    public async Task<IActionResult> Save([FromBody] SaveCommissionConfigRequest request)
    {
        var result = await _commissionConfigService.SaveAsync(request);
        return Ok(result, _localizer["Commission_SaveSuccess"]);
    }

    /// <summary>Xoá một cấu hình hoa hồng.</summary>
    [HttpDelete("{id:guid}")]
    [HasPermission(PermissionCode.UpdateCommissionConfigs)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _commissionConfigService.DeleteAsync(id);
        return Ok(new { Id = id }, _localizer["Commission_DeleteSuccess"]);
    }

    /// <summary>Tìm tài khoản để chọn khi cấu hình hoa hồng riêng.</summary>
    [HttpGet("users")]
    [HasPermission(PermissionCode.ViewCommissionConfigs)]
    public async Task<IActionResult> SearchUsers([FromQuery] string? search)
    {
        var result = await _commissionConfigService.SearchUsersAsync(search);
        return Ok(result, _localizer["Commission_ListSuccess"]);
    }

    /// <summary>Mức hoa hồng đang áp cho chính người gọi (bản riêng nếu có, không thì bản chung).</summary>
    [HttpGet("me")]
    [HasPermission(PermissionCode.ViewMyCommission)]
    public async Task<IActionResult> GetMine()
    {
        var result = await _commissionConfigService.GetMineAsync();
        return Ok(result, _localizer["Commission_MySuccess"]);
    }
}
