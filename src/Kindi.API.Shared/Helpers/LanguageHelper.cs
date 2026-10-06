namespace Kindi.API.Shared.Common.Helpers;

/// <summary>
/// Chuẩn hoá ngôn ngữ dùng cho claim trong token và culture của request.
/// Nguồn duy nhất về ngôn ngữ là người dùng chọn lúc đăng nhập → API ghi vào token.
/// </summary>
public static class LanguageHelper
{
    /// <summary>Ngôn ngữ mặc định khi token không có claim ngôn ngữ.</summary>
    public const string Default = "vi";

    /// <summary>
    /// 'vi', 'vi-VN', 'VI', 'en_US' → 'vi' | 'en'; giá trị rỗng hoặc ngôn ngữ API không hỗ trợ trả <c>null</c>
    /// để nơi gọi giữ nguyên ngôn ngữ hiện hành.
    /// </summary>
    public static string? Normalize(string? language)
    {
        if (string.IsNullOrWhiteSpace(language)) return null;

        var primary = language.Trim().Replace('_', '-').Split('-')[0].ToLowerInvariant();
        return primary is "vi" or "en" ? primary : null;
    }
}
