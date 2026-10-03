namespace Kindi.API.Application.DTOs.requests;

/// <summary>Bộ quyền hiệu lực đặt cho một tài khoản (danh sách mã P### mong muốn).</summary>
public class UpdateUserPermissionsRequest
{
    public List<string> PermissionCodes { get; set; } = new();
}

/// <summary>Bộ quyền hiệu lực áp cho nhiều tài khoản được chọn cùng lúc.</summary>
public class UpdateUsersPermissionsRequest
{
    public List<Guid> UserIds { get; set; } = new();

    public List<string> PermissionCodes { get; set; } = new();
}
