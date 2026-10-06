namespace Kindi.API.Shared.Constants;

/// <summary>
/// Vai trò dùng cho phân quyền — giá trị số khớp enum <c>UserRole</c> của Domain
/// (User = 1, Partner = 2, Admin = 3, SuperAdmin = 4). Giữ dạng chuỗi vì phải là const để dùng
/// trong <c>[Authorize(Roles = ...)]</c> và claim vai trò của JWT.
/// </summary>
public static class RoleConstants
{
    public const string User = "1";
    public const string Partner = "2";
    public const string Admin = "3";
    public const string SuperAdmin = "4";
}

/// <summary>Tên claim dùng trong JWT.</summary>
public static class AuthClaimConstants
{
    /// <summary>Danh sách mã quyền (P###), cách nhau bằng dấu phẩy.</summary>
    public const string Permissions = "perm";

    /// <summary>Phiên bản quyền của role — đổi quyền thì tăng số này để client biết cần refresh token.</summary>
    public const string PermissionsVersion = "permv";

    /// <summary>
    /// Ngôn ngữ người dùng chọn lúc đăng nhập (vi | en) — API trả nội dung dịch theo claim này,
    /// không phải chờ client gửi header Accept-Language ở từng request.
    /// </summary>
    public const string Language = "lang";
}

public static class PolicyConstants
{
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireUser = "RequireUser";

    /// <summary>Tiền tố policy sinh động cho quyền P### (xem PermissionPolicyProvider).</summary>
    public const string PermissionPrefix = "permission:";
}
