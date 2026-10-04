namespace Kindi.API.Domain.Enums;

using System.Collections.Frozen;

/// <summary>
/// Khoá dịch của từng node trong cây quyền. UI dùng <c>nameKey</c> để tra bản dịch trong resource
/// (SharedResource.vi/en.resx) thay vì hiển thị trực tiếp tên tiếng Việt đã seed trong DB.
/// </summary>
public static class PermissionNameKeys
{
    /// <summary>Khoá dịch của node nhóm: <c>PermissionGroup_&lt;MÃ&gt;</c>.</summary>
    public static string Group(string code) => $"PermissionGroup_{code}";

    /// <summary>Khoá dịch của node màn hình: <c>PermissionScreen_&lt;MÃ&gt;</c>.</summary>
    public static string Screen(string code) => $"PermissionScreen_{code}";

    /// <summary>Khoá dịch của node hành động: <c>Permission_&lt;P###&gt;</c>.</summary>
    public static string Action(string permissionCode) => $"Permission_{permissionCode}";
}

/// <summary>
/// Cấu trúc tĩnh của cây quyền (Nhóm → Màn hình → Hành động) lấy trực tiếp từ các danh mục trong code.
/// Dùng để: dựng endpoint <c>GET /Permissions/tree</c>, và kiểm quyền có KẾ THỪA (tắt cha ⇒ con mất hiệu lực).
/// Toàn bộ quan hệ cha–con nằm trong code nên không cần truy vấn DB khi kiểm kế thừa.
/// </summary>
public static class PermissionTreeCatalog
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    /// <summary>Mã các node nhóm (gốc) theo thứ tự hiển thị.</summary>
    public static IReadOnlyList<PermissionGroupDefinition> Groups => PermissionGroupCatalog.All;

    /// <summary>Mã các node màn hình theo thứ tự hiển thị.</summary>
    public static IReadOnlyList<PermissionScreenDefinition> Screens => PermissionScreenCatalog.All;

    /// <summary>Toàn bộ mã node (nhóm + màn hình + hành động) — tập hợp quyền được phép gán.</summary>
    public static IReadOnlySet<string> AllCodes { get; } = BuildAllCodes();

    /// <summary>
    /// Chuỗi tổ tiên (gần → xa) của một node: hành động → [màn hình, nhóm]; màn hình → [nhóm]; nhóm → rỗng.
    /// Trả về rỗng cho mã lạ.
    /// </summary>
    public static IReadOnlyList<string> AncestorsOf(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return Array.Empty<string>();

        if (PermissionCatalog.TryGet(code, out var definition))
        {
            var chain = new List<string>(2);
            if (!string.IsNullOrWhiteSpace(definition.Screen)) chain.Add(definition.Screen);
            var screen = PermissionScreenCatalog.Resolve(definition.Screen);
            if (screen is not null) chain.Add(screen.Group);
            return chain;
        }

        var screenNode = PermissionScreenCatalog.Resolve(code);
        if (screenNode is not null) return new[] { screenNode.Group };

        return Array.Empty<string>();
    }

    /// <summary>Node có phải gốc (nhóm) không.</summary>
    public static bool IsGroup(string code) => PermissionGroupCatalog.All.Any(x => Comparer.Equals(x.Code, code));

    /// <summary>Node có phải màn hình không.</summary>
    public static bool IsScreen(string code) => PermissionScreenCatalog.Resolve(code) is not null;

    /// <summary>Loại node của một mã.</summary>
    public static PermissionNodeKind NodeKindOf(string code)
    {
        if (IsGroup(code)) return PermissionNodeKind.Group;
        if (IsScreen(code)) return PermissionNodeKind.Screen;
        return PermissionNodeKind.Action;
    }

    /// <summary>
    /// KẾ THỪA: quyền hiệu lực = chính node được cấp VÀ mọi tổ tiên đều được cấp.
    /// Tắt một node cha (không cấp) ⇒ mọi node con bị loại khỏi tập hiệu lực dù node con vẫn còn được cấp.
    /// Đây là phép tính thuần trên danh mục tĩnh nên có thể cache/không truy vấn DB.
    /// </summary>
    public static IReadOnlySet<string> ApplyInheritance(IEnumerable<string> grantedCodes)
    {
        var granted = grantedCodes as IReadOnlySet<string> ?? grantedCodes.ToHashSet(Comparer);
        var effective = new HashSet<string>(Comparer);

        foreach (var code in granted)
        {
            var ancestors = AncestorsOf(code);
            var allowed = true;
            foreach (var ancestor in ancestors)
            {
                if (!granted.Contains(ancestor))
                {
                    allowed = false;
                    break;
                }
            }

            if (allowed) effective.Add(code);
        }

        return effective;
    }

    /// <summary>
    /// Tập mã node con (trực tiếp và sâu hơn) của một node — dùng cho báo cáo/thống kê.
    /// </summary>
    public static IReadOnlyList<string> DescendantsOf(string code)
    {
        var result = new List<string>();
        var stack = new Stack<string>();
        stack.Push(code);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            foreach (var child in AllCodes.Where(c => AncestorsOf(c).Any(a => Comparer.Equals(a, current))))
            {
                result.Add(child);
                stack.Push(child);
            }
        }

        return result;
    }

    private static IReadOnlySet<string> BuildAllCodes()
    {
        var codes = new HashSet<string>(Comparer);
        foreach (var group in PermissionGroupCatalog.All) codes.Add(group.Code);
        foreach (var screen in PermissionScreenCatalog.All) codes.Add(screen.Code);
        foreach (var definition in PermissionCatalog.All) codes.Add(definition.PermissionCode);
        return codes.ToFrozenSet(Comparer);
    }
}
