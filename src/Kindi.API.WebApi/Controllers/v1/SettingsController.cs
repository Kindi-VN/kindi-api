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
/// Cài đặt chung của hệ thống: thông tin nền tảng, ngôn ngữ — định dạng, chính sách đăng ký — tài khoản,
/// giới hạn tệp — nội dung và thiết lập thông báo.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class SettingsController : ApiControllerBase
{
    private readonly ISystemSettingService _systemSettingService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public SettingsController(
        ISystemSettingService systemSettingService,
        IStringLocalizer<SharedResource> localizer)
    {
        _systemSettingService = systemSettingService;
        _localizer = localizer;
    }

    /// <summary>Cài đặt chung đầy đủ dùng cho màn quản trị.</summary>
    [HttpGet]
    [HasPermission(PermissionCode.ViewSystemSettings)]
    public async Task<IActionResult> Get()
    {
        var result = await _systemSettingService.GetAsync();
        return Ok(result, _localizer["SystemSetting_Success"]);
    }

    /// <summary>Giá trị mặc định của cài đặt chung (cho nút khôi phục mặc định).</summary>
    [HttpGet("defaults")]
    [HasPermission(PermissionCode.ViewSystemSettings)]
    public async Task<IActionResult> GetDefaults()
    {
        var result = await _systemSettingService.GetDefaultsAsync();
        return Ok(result, _localizer["SystemSetting_DefaultsSuccess"]);
    }

    /// <summary>Khôi phục toàn bộ cài đặt chung về giá trị mặc định.</summary>
    [HttpPost("reset")]
    [HasPermission(PermissionCode.UpdateSystemSettings)]
    public async Task<IActionResult> Reset()
    {
        var result = await _systemSettingService.ResetAsync();
        return Ok(result, _localizer["SystemSetting_ResetSuccess"]);
    }

    /// <summary>Phần cài đặt công khai cho giao diện người dùng (không cần đăng nhập).</summary>
    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublic()
    {
        var result = await _systemSettingService.GetPublicAsync();
        return Ok(result, _localizer["SystemSetting_PublicSuccess"]);
    }

    /// <summary>Cập nhật cài đặt chung.</summary>
    [HttpPut]
    [HasPermission(PermissionCode.UpdateSystemSettings)]
    public async Task<IActionResult> Save([FromBody] SaveSystemSettingRequest request)
    {
        var result = await _systemSettingService.SaveAsync(request);
        return Ok(result, _localizer["SystemSetting_SavedSuccess"]);
    }
}
