namespace Kindi.API.Application.DTOs.responses;

/// <summary>Một quyền trong danh mục (mã P### kèm view/API mà quyền đó mở).</summary>
public class PermissionItemResponse
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string? Route { get; set; }
    public string? Endpoints { get; set; }
}

/// <summary>Quyền đang bật của một vai trò.</summary>
public class RolePermissionResponse
{
    public string Role { get; set; } = string.Empty;

    /// <summary>SuperAdmin luôn có toàn quyền, không cấu hình được.</summary>
    public bool IsSuperAdmin { get; set; }

    public IReadOnlyList<string> PermissionCodes { get; set; } = new List<string>();
}

/// <summary>Ma trận phân quyền: danh mục quyền + quyền của từng vai trò.</summary>
public class PermissionMatrixResponse
{
    public IReadOnlyList<PermissionItemResponse> Permissions { get; set; } = new List<PermissionItemResponse>();

    public IReadOnlyList<RolePermissionResponse> Roles { get; set; } = new List<RolePermissionResponse>();
}
