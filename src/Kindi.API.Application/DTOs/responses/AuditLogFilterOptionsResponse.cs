namespace Kindi.API.Application.DTOs.responses;

using Kindi.API.Shared.Constants;

/// <summary>Danh mục chọn nhanh cho bộ lọc nhật ký hoạt động (droplist hành động + tên bảng).</summary>
public class AuditLogFilterOptionsResponse
{
    /// <summary>Tên bảng đã phát sinh nhật ký (lấy từ dữ liệu thật).</summary>
    public IReadOnlyList<string> EntityNames { get; set; } = new List<string>();

    /// <summary>Hành động của nhật ký thao tác dữ liệu.</summary>
    public IReadOnlyList<string> EntityActions { get; set; } = new List<string>();

    /// <summary>Hành động của nhật ký xác thực tài khoản (đăng nhập/đăng ký/đổi mật khẩu...).</summary>
    public IReadOnlyList<string> AuthActions { get; set; } = new List<string>();

    /// <summary>
    /// Dựng danh mục lọc: hành động lấy từ hằng số <see cref="AuditAction"/>, tên bảng lấy từ dữ liệu
    /// (đã bỏ rỗng/trùng và sắp xếp).
    /// </summary>
    public static AuditLogFilterOptionsResponse Build(IEnumerable<string>? entityNames) => new()
    {
        EntityNames = Normalize(entityNames),
        EntityActions = new List<string>
        {
            AuditAction.Create,
            AuditAction.Update,
            AuditAction.Delete
        },
        AuthActions = new List<string>
        {
            AuditAction.Login,
            AuditAction.Register,
            AuditAction.RefreshToken,
            AuditAction.ChangePassword,
            AuditAction.ResetPassword,
            AuditAction.Logout
        }
    };

    /// <summary>Bỏ giá trị rỗng, bỏ trùng (không phân biệt hoa/thường) và sắp xếp A→Z cho droplist.</summary>
    private static List<string> Normalize(IEnumerable<string>? values)
        => values?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();
}
