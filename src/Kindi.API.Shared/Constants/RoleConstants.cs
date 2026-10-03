namespace Kindi.API.Shared.Constants;

/// <summary>
/// Vai trò dùng cho phân quyền — giá trị số khớp enum <c>UserRole</c> của Domain
/// (Customer = 1, CTV = 2, Admin = 3). Giữ dạng chuỗi vì phải là const để dùng
/// trong <c>[Authorize(Roles = ...)]</c> và claim vai trò của JWT.
/// </summary>
public static class RoleConstants
{
    public const string Customer = "1";
    public const string CTV = "2";
    public const string Admin = "3";
}

public static class PolicyConstants
{
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireUser = "RequireUser";
}
