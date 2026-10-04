using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Application.Resources;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Rules;
using Kindi.API.WebApi.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Kindi.API.Shared.Constants;

namespace Kindi.API.WebApi.Controllers.Admin;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/permissions")]
[Authorize(Roles = RoleConstants.Admin)]
[ApiController]
public class PermissionsController : ApiControllerBase
{
    private readonly IPermissionService _permissionService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public PermissionsController(
        IPermissionService permissionService,
        IStringLocalizer<SharedResource> localizer)
    {
        _permissionService = permissionService;
        _localizer = localizer;
    }

    /// <summary>
    /// Ma trận phân quyền: danh mục quyền (theo enum PermissionCode) và quyền đang bật của từng vai trò.
    /// </summary>
    [HttpGet]
    [HasPermission(PermissionCode.ViewPermissions)]
    public async Task<IActionResult> GetMatrix()
    {
        var matrix = await BuildMatrixAsync();
        return Ok(matrix, _localizer["Permissions_ListSuccess"]);
    }

    /// <summary>
    /// Cây quyền đệ quy: Nhóm → Màn hình → Hành động. Mỗi node trả <c>code</c>, <c>nameKey</c>,
    /// <c>kind</c> (group|screen|action), <c>parentCode</c>, <c>children</c>, kèm <c>isGranted</c>
    /// (tick trực tiếp) và <c>isEffective</c> (hiệu lực sau kế thừa) của vai trò đang xét.
    /// </summary>
    /// <param name="role">Giá trị số của vai trò (1 User, 2 Partner, 3 Admin, 8 SuperAdmin). Bỏ trống = Admin.</param>
    [HttpGet("tree")]
    [HasPermission(PermissionCode.ViewPermissions)]
    public async Task<IActionResult> GetTree([FromQuery] int? role = null)
    {
        var tree = await _permissionService.GetTreeAsync(role);
        return Ok(tree, _localizer["Permissions_ListSuccess"]);
    }

    /// <summary>
    /// Cập nhật danh sách quyền được bật cho một vai trò (chỉ SuperAdmin có quyền P101).
    /// </summary>
    [HttpPut("roles/{role:int}")]
    [HasPermission(PermissionCode.UpdateRolePermissions)]
    public async Task<IActionResult> UpdateRolePermissions(int role, [FromBody] UpdateRolePermissionsRequest request)
    {
        if (!Enum.IsDefined(typeof(UserRole), role))
            return BadRequest(_localizer["Permissions_RoleNotAssignable"]);

        var userRole = (UserRole)role;
        if (!RoleRules.IsAssignable(userRole))
            return BadRequest(_localizer["Permissions_RoleNotAssignable"]);

        // Hợp lệ cả node hành động (P###) lẫn node màn hình/nhóm — để UI lưu được cả trạng thái bật/tắt node cha.
        var known = PermissionTreeCatalog.AllCodes;
        var unknown = request.PermissionCodes
            .Where(code => !known.Contains(code))
            .ToList();
        if (unknown.Count > 0)
            return BadRequest(_localizer["Permissions_UnknownCodes", string.Join(", ", unknown)]);

        await _permissionService.SetRolePermissionsAsync(userRole, request.PermissionCodes);

        var matrix = await BuildMatrixAsync();
        return Ok(matrix, _localizer["Permissions_UpdateSuccess"]);
    }

    /// <summary>Tìm tài khoản để cấu hình quyền riêng (không gồm tài khoản SuperAdmin).</summary>
    [HttpGet("users")]
    [HasPermission(PermissionCode.UpdateUserPermissions)]
    public async Task<IActionResult> GetUserCandidates([FromQuery] string? search, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var users = await _permissionService.SearchUsersAsync(search, pageNumber, pageSize);
        var data = users
            .Select(x => new UserPermissionCandidateResponse
            {
                Id = x.Id,
                Username = x.Username,
                FullName = x.FullName,
                Role = ((int)x.Role).ToString(),
                RoleName = x.Role.ToString()
            })
            .ToList();

        return Ok(data, _localizer["Permissions_UsersListSuccess"]);
    }

    /// <summary>Xem quyền hiệu lực và phần cấu hình riêng của một tài khoản.</summary>
    [HttpGet("users/{userId:guid}")]
    [HasPermission(PermissionCode.UpdateUserPermissions)]
    public async Task<IActionResult> GetUserPermissions(Guid userId)
    {
        var detail = await _permissionService.GetUserPermissionDetailAsync(userId);
        if (detail == null)
            return NotFound(_localizer["Permissions_UserNotFound"]);

        return Ok(BuildUserPermissionDetail(detail), _localizer["Permissions_ListSuccess"]);
    }

    /// <summary>Cập nhật quyền hiệu lực cho một tài khoản (chỉ lưu phần khác biệt so với role).</summary>
    [HttpPut("users/{userId:guid}")]
    [HasPermission(PermissionCode.UpdateUserPermissions)]
    public async Task<IActionResult> UpdateUserPermissions(Guid userId, [FromBody] UpdateUserPermissionsRequest request)
    {
        var unknown = FindUnknownCodes(request.PermissionCodes);
        if (unknown.Count > 0)
            return BadRequest(_localizer["Permissions_UnknownCodes", string.Join(", ", unknown)]);

        try
        {
            await _permissionService.SetUserPermissionsAsync(new[] { userId }, request.PermissionCodes);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(exception.Message);
        }

        var detail = await _permissionService.GetUserPermissionDetailAsync(userId);
        if (detail == null)
            return NotFound(_localizer["Permissions_UserNotFound"]);

        return Ok(BuildUserPermissionDetail(detail), _localizer["Permissions_UserUpdateSuccess"]);
    }

    /// <summary>Áp cùng một bộ quyền cho nhiều tài khoản được chọn.</summary>
    [HttpPut("users")]
    [HasPermission(PermissionCode.UpdateUserPermissions)]
    public async Task<IActionResult> UpdateUsersPermissions([FromBody] UpdateUsersPermissionsRequest request)
    {
        if (request.UserIds.Count == 0)
            return BadRequest(_localizer["Permissions_NoUsersSelected"]);

        var unknown = FindUnknownCodes(request.PermissionCodes);
        if (unknown.Count > 0)
            return BadRequest(_localizer["Permissions_UnknownCodes", string.Join(", ", unknown)]);

        try
        {
            await _permissionService.SetUserPermissionsAsync(request.UserIds, request.PermissionCodes);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(exception.Message);
        }

        return Ok(new UpdateUsersPermissionsResponse { UpdatedUsers = request.UserIds.Distinct().Count() },
            _localizer["Permissions_UserUpdateSuccess"]);
    }

    /// <summary>Mã quyền gửi lên không có trong danh mục.</summary>
    private List<string> FindUnknownCodes(IEnumerable<string> permissionCodes)
    {
        // Hợp lệ cả node hành động (P###) lẫn node màn hình/nhóm — để UI lưu được cả trạng thái bật/tắt node cha.
        var known = PermissionTreeCatalog.AllCodes;

        return permissionCodes.Where(code => !known.Contains(code)).ToList();
    }

    private static UserPermissionDetailResponse BuildUserPermissionDetail(UserPermissionDetail detail)
        => new()
        {
            UserId = detail.UserId,
            Username = detail.Username,
            FullName = detail.FullName,
            Role = ((int)detail.Role).ToString(),
            RoleName = detail.Role.ToString(),
            RolePermissionCodes = detail.RoleCodes.OrderBy(x => x, StringComparer.Ordinal).ToList(),
            GrantedCodes = detail.GrantedCodes.OrderBy(x => x, StringComparer.Ordinal).ToList(),
            DeniedCodes = detail.DeniedCodes.OrderBy(x => x, StringComparer.Ordinal).ToList(),
            EffectiveCodes = detail.EffectiveCodes.OrderBy(x => x, StringComparer.Ordinal).ToList()
        };

    private async Task<PermissionMatrixResponse> BuildMatrixAsync()
    {
        var permissions = _permissionService.GetCatalog()
            .Select(x => new PermissionItemResponse
            {
                Code = x.PermissionCode,
                Name = x.Name,
                Module = x.Module.ToString(),
                Kind = x.Kind.ToString(),
                Route = x.Route,
                Endpoints = x.Endpoints,
                ParentCode = x.Group,
                Screen = x.Screen,
                ScreenName = x.ScreenName
            })
            .ToList();

        var groups = (await _permissionService.GetGroupsAsync())
            .Select(x => new PermissionGroupResponse
            {
                Code = x.Code,
                Name = x.Name,
                NameEn = x.NameEn,
                SortOrder = x.SortOrder
            })
            .ToList();

        var roles = new List<RolePermissionResponse>();
        foreach (var role in new[] { UserRole.User, UserRole.Partner, UserRole.Admin, UserRole.SuperAdmin })
        {
            var granted = await _permissionService.GetRolePermissionsAsync(role);
            roles.Add(new RolePermissionResponse
            {
                Role = ((int)role).ToString(),
                IsSuperAdmin = role == UserRole.SuperAdmin,
                PermissionCodes = granted.Codes.OrderBy(x => x, StringComparer.Ordinal).ToList()
            });
        }

        return new PermissionMatrixResponse
        {
            Permissions = permissions,
            Groups = groups,
            Roles = roles
        };
    }
}
