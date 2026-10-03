namespace Kindi.API.Application.DTOs.requests;

/// <summary>Danh sách mã quyền (P###) được bật cho một vai trò.</summary>
public class UpdateRolePermissionsRequest
{
    public List<string> PermissionCodes { get; set; } = new();
}
