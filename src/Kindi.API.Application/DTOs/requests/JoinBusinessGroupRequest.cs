namespace Kindi.API.Application.DTOs.requests;

/// <summary>
/// Xin vào nhóm. Đã đăng nhập thì không cần thông tin liên hệ; khách chưa có tài khoản
/// thì bắt buộc Họ tên + SĐT để hệ thống tạo tài khoản (như luồng mua chung).
/// </summary>
public class JoinBusinessGroupRequest
{
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Zalo { get; set; }
    public string? Email { get; set; }
    public string? Note { get; set; }

    /// <summary>Mã CTV của link chia sẻ khách dùng để xin vào nhóm (không bắt buộc).</summary>
    public string? ReferralCode { get; set; }
}
