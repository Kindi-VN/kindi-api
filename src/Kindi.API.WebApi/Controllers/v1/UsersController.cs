using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.Resources;
using Kindi.API.Shared.Constants;
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
    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id)
    {
        var result = await _userService.ResetPasswordToPhoneAsync(id);

        if (result == null)
            return NotFound(_localizer["User_NotFound"]);

        return Ok(result, _localizer["User_ResetPasswordSuccess"]);
    }
}
