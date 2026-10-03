namespace Kindi.API.Application.DTOs.responses;

/// <summary>Tài khoản chọn được ở màn cấu hình quyền riêng.</summary>
public class UserPermissionCandidateResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    /// <summary>Giá trị số của role (1 User, 2 Partner, 3 Admin).</summary>
    public string Role { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;
}

/// <summary>Quyền của một tài khoản: quyền theo role, phần bật thêm, phần tắt riêng và quyền hiệu lực.</summary>
public class UserPermissionDetailResponse
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public List<string> RolePermissionCodes { get; set; } = new();
    public List<string> GrantedCodes { get; set; } = new();
    public List<string> DeniedCodes { get; set; } = new();
    public List<string> EffectiveCodes { get; set; } = new();
}

/// <summary>Kết quả áp quyền cho nhiều tài khoản.</summary>
public class UpdateUsersPermissionsResponse
{
    public int UpdatedUsers { get; set; }
}
