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

/// <summary>
/// Một node trong cây quyền đệ quy: Nhóm (group) → Màn hình (screen) → Hành động (action).
/// <see cref="NameKey"/> là khoá dịch để UI hiển thị tên node; <see cref="IsGranted"/> là trạng thái cấp
/// trực tiếp, <see cref="IsEffective"/> là hiệu lực sau kế thừa (bản thân + mọi tổ tiên đều được cấp).
/// </summary>
public class PermissionTreeNodeResponse
{
    /// <summary>Mã node: hành động P###, màn hình/nhóm mã chữ.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Khoá dịch của tên node (ví dụ Permission_P020, PermissionScreen_OFFERS, PermissionGroup_ADMIN).</summary>
    public string NameKey { get; set; } = string.Empty;

    /// <summary>
    /// Tên hiển thị ĐÃ DỊCH theo ngôn ngữ của request (API tự tra resx theo <see cref="NameKey"/>) — UI chỉ
    /// cần hiển thị, không phải giữ bản dịch riêng cho từng mã quyền.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Loại hành động (view | action | update | delete) — chỉ có ở node hành động.</summary>
    public string? ActionKind { get; set; }

    /// <summary>Module nghiệp vụ của quyền — chỉ có ở node hành động.</summary>
    public string? Module { get; set; }

    /// <summary>Route UI mà quyền này mở (nếu có) — hiển thị cho quản trị biết quyền dùng ở đâu.</summary>
    public string? Route { get; set; }

    /// <summary>Các endpoint API nằm dưới quyền này (mô tả, cách nhau bằng dấu phẩy).</summary>
    public string? Endpoints { get; set; }

    /// <summary>Loại node: <c>group</c> | <c>screen</c> | <c>action</c>.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Mã node cha (null với node nhóm gốc).</summary>
    public string? ParentCode { get; set; }

    /// <summary>Node đang được tick trực tiếp cho vai trò đang xét.</summary>
    public bool IsGranted { get; set; }

    /// <summary>Node có hiệu lực sau kế thừa (chính nó và mọi tổ tiên đều được cấp).</summary>
    public bool IsEffective { get; set; }

    /// <summary>Các node con trực tiếp.</summary>
    public List<PermissionTreeNodeResponse> Children { get; set; } = new();
}
