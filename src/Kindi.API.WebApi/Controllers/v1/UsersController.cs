using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.Resources;
using Kindi.API.Shared.Constants;
using Kindi.API.WebApi.Authorization;
using Kindi.API.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Kindi.API.WebApi.Controllers;

/// <summary>
/// Quản lý người dùng — chỉ dành cho quản trị.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = RoleConstants.Admin)]
public class UsersController : ApiControllerBase
{
    private readonly IUserService _userService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public UsersController(IUserService userService, IStringLocalizer<SharedResource> localizer)
    {
        _userService = userService;
        _localizer = localizer;
    }

    /// <summary>
    /// Danh sách người dùng phân trang
    /// GET /api/v1/Users?pageNumber=&pageSize=&search=
    /// </summary>
    [HasPermission(PermissionCode.ViewUsers)]
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] UserQueryDto query)
    {
        var result = await _userService.GetPagedAsync(query);
        return OkPaged(result);
    }

    /// <summary>
    /// Cấp lại mật khẩu cho người dùng: mật khẩu mới là số điện thoại của tài khoản,
    /// bắt buộc đổi ở lần đăng nhập kế tiếp (dùng khi người dùng quên mật khẩu).
    /// </summary>
    [HasPermission(PermissionCode.ResetUserPassword)]
    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id)
    {
        var result = await _userService.ResetPasswordToPhoneAsync(id);

        if (result == null)
            return NotFound(_localizer["User_NotFound"]);

        return Ok(result, _localizer["User_ResetPasswordSuccess"]);
    }

    /// <summary>
    /// Chi tiết một tài khoản cho màn quản lý: thông tin tài khoản + hồ sơ CTV/đối tác đang liên kết
    /// (một điểm xem, sửa tập trung).
    /// </summary>
    [HasPermission(PermissionCode.ViewUserDetail)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDetail(Guid id)
    {
        var result = await _userService.GetDetailAsync(id);

        if (result == null)
            return NotFound(_localizer["User_NotFound"]);

        return Ok(result, _localizer["User_DetailSuccess"]);
    }

    /// <summary>
    /// Sửa thông tin tài khoản (họ tên / SĐT / email / Zalo / trạng thái hoạt động) — điểm ghi tập trung
    /// cho cả tài khoản thường lẫn tài khoản quản trị.
    /// </summary>
    [HasPermission(PermissionCode.UpdateUserInfo)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateInfo(Guid id, [FromBody] UpdateUserInfoRequest request)
    {
        var result = await _userService.UpdateInfoAsync(id, request);

        if (result == null)
            return NotFound(_localizer["User_NotFound"]);

        return Ok(result, _localizer["User_UpdateInfoSuccess"]);
    }

    /// <summary>Tạo tài khoản quản trị (role Admin) — mật khẩu do người tạo đặt, không bắt đổi lần đầu.</summary>
    [HasPermission(PermissionCode.CreateAdminAccount)]
    [HttpPost("admin")]
    public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminUserRequest request)
    {
        var result = await _userService.CreateAdminAsync(request);

        return Ok(result, _localizer["User_CreateAdminSuccess"]);
    }

    /// <summary>Gán / đổi vai trò tài khoản (không gán được SuperAdmin).</summary>
    [HasPermission(PermissionCode.AssignUserRole)]
    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateUserRoleRequest request)
    {
        var result = await _userService.UpdateRoleAsync(id, request.Role);

        if (result == null)
            return NotFound(_localizer["User_NotFound"]);

        return Ok(result, _localizer["User_UpdateRoleSuccess"]);
    }
}
