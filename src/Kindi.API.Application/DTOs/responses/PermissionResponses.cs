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

    /// <summary>Mã nhóm quyền (PermissionGroups.Code) dùng để gom nhóm ở màn phân quyền.</summary>
    public string? ParentCode { get; set; }

    /// <summary>Mã màn hình — gom các hành động của cùng màn hình khi render Nhóm → Màn hình → hành động.</summary>
    public string Screen { get; set; } = string.Empty;

    /// <summary>Tên hiển thị của màn hình.</summary>
    public string ScreenName { get; set; } = string.Empty;
}

/// <summary>Nhóm quyền: mã, tên hiển thị và thứ tự (đọc từ bảng PermissionGroups).</summary>
public class PermissionGroupResponse
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int SortOrder { get; set; }
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

    /// <summary>Danh sách nhóm quyền kèm tên hiển thị và thứ tự.</summary>
    public IReadOnlyList<PermissionGroupResponse> Groups { get; set; } = new List<PermissionGroupResponse>();

    public IReadOnlyList<RolePermissionResponse> Roles { get; set; } = new List<RolePermissionResponse>();
}
