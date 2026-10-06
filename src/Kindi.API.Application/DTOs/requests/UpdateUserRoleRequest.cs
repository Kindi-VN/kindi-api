namespace Kindi.API.Application.DTOs.requests;

using Kindi.API.Domain.Enums;

/// <summary>Gán / đổi vai trò của một tài khoản (SuperAdmin không gán được — xem <c>RoleRules</c>).</summary>
public class UpdateUserRoleRequest
{
    public UserRole Role { get; set; }
}
