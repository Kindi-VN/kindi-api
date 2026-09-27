using System.Globalization;
using System.Text;

namespace Kindi.API.Application.Common.Extensions;

/// <summary>
/// Extension cho string: so sánh không phân biệt hoa/thường và chuẩn hoá tiếng Việt.
/// </summary>
public static class StringExtensions
{
    /// <summary>So sánh bằng nhau không phân biệt hoa/thường (ordinal).</summary>
    public static bool EqualsIgnoreCase(this string? value, string? other)
        => string.Equals(value, other, StringComparison.OrdinalIgnoreCase);

    /// <summary>Kiểm tra chứa chuỗi con, không phân biệt hoa/thường (ordinal).</summary>
    public static bool ContainsIgnoreCase(this string? source, string? value)
        => !string.IsNullOrEmpty(source)
           && !string.IsNullOrEmpty(value)
           && source.Contains(value, StringComparison.OrdinalIgnoreCase);

    /// <summary>Kiểm tra bắt đầu bằng, không phân biệt hoa/thường (ordinal).</summary>
    public static bool StartsWithIgnoreCase(this string? source, string? value)
        => !string.IsNullOrEmpty(source)
           && !string.IsNullOrEmpty(value)
           && source.StartsWith(value, StringComparison.OrdinalIgnoreCase);

    /// <summary>Kiểm tra kết thúc bằng, không phân biệt hoa/thường (ordinal).</summary>
    public static bool EndsWithIgnoreCase(this string? source, string? value)
        => !string.IsNullOrEmpty(source)
           && !string.IsNullOrEmpty(value)
           && source.EndsWith(value, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Bỏ dấu tiếng Việt (Sữa bột → Sua bot), giữ nguyên chữ và khoảng trắng.
    /// </summary>
    public static string RemoveVietnameseSign(this string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            // Dấu thanh/dấu mũ tách rời khi phân rã, bỏ qua để còn lại ký tự gốc không dấu.
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                continue;

            builder.Append(character switch
            {
                'đ' => 'd',
                'Đ' => 'D',
                _ => character
            });
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Khoá tìm kiếm trong bộ nhớ: bỏ dấu tiếng Việt, viết thường, gộp khoảng trắng.
    /// </summary>
    public static string ToSearchKey(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var words = value.RemoveVietnameseSign()
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return string.Join(' ', words);
    }

    /// <summary>
    /// Chuẩn hoá từ khoá lọc do client gửi lên: bỏ khoảng trắng thừa và các giá trị giữ chỗ
    /// (undefined/null/nan/all) coi như không lọc.
    /// </summary>
    public static string? NormalizeSearchFilter(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.ToLowerInvariant() switch
        {
            "undefined" or "null" or "nan" or "all" => null,
            _ => trimmed
        };
    }

    /// <summary>
    /// Escape ký tự đại diện trong từ khoá tìm kiếm: \, % và _ (dùng cho mẫu LIKE/ILIKE dưới DB).
    /// Chỉ trả về từ khoá đã escape, KHÔNG bọc sẵn hai dấu % — phía truy vấn tự ghép
    /// "%" + term + "%" ngay trong biểu thức để EF dịch thành chuỗi nối dưới SQL.
    /// LƯU Ý: Npgsql sinh ILIKE mặc định là ESCAPE '' (TẮT escape), nên khi dùng từ khoá đã
    /// escape bắt buộc truyền escape char: EF.Functions.ILike(col, pattern, "\\").
    /// </summary>
    public static string ToLikeEscaped(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Trim()
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}
