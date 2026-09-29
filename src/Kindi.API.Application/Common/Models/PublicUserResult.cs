namespace Kindi.API.Application.Common.Models;

/// <summary>
/// Kết quả xử lý tài khoản cho luồng công khai (khách chưa đăng nhập).
/// </summary>
public record PublicUserResult(Guid UserId, bool IsNewAccount)
{
    /// <summary>Tài khoản vừa được tạo tự động từ form.</summary>
    public bool IsGuestAccount => IsNewAccount;
}
