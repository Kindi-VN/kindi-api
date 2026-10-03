namespace Kindi.API.Shared.Common.Helpers;

/// <summary>
/// Chuẩn hoá số điện thoại để đối chiếu giữa các luồng (đăng nhập, đăng ký, tìm tài khoản).
/// </summary>
public static class PhoneHelper
{
    /// Bỏ mọi ký tự không phải chữ số và đổi tiền tố quốc gia 84 thành 0 (vd: "+84 912 345 678" → "0912345678").
    public static string Normalize(string? phone)
    {
        var digits = string.Concat((phone ?? string.Empty).Where(char.IsDigit));
        if (digits.Length == 11 && digits.StartsWith("84"))
            digits = "0" + digits[2..];
        return digits;
    }

    /// Các dạng có thể gặp của số điện thoại người dùng nhập: bản đã trim và bản đã chuẩn hoá.
    public static List<string> Candidates(string? phone)
    {
        var trimmed = phone?.Trim();
        var normalized = Normalize(phone);
        var candidates = new List<string>();

        if (!string.IsNullOrEmpty(trimmed))
            candidates.Add(trimmed);
        if (!string.IsNullOrEmpty(normalized) && !candidates.Contains(normalized))
            candidates.Add(normalized);

        return candidates;
    }
}
