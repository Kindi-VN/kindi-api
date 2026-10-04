namespace Kindi.API.Application.DTOs.requests;

/// <summary>
/// Thông tin đăng ký tham gia một nhóm mua chung.
/// Người đã đăng nhập chỉ cần gửi <see cref="Note"/>; khách chưa đăng nhập phải gửi đủ
/// họ tên + số điện thoại (hệ thống tạo tài khoản từ chính thông tin này).
/// </summary>
public class JoinGroupBuyingRequestDto
{
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Zalo { get; set; }
    public string? Email { get; set; }
    public string? Note { get; set; }

    /// <summary>Mã CTV của link chia sẻ khách dùng để tham gia (không bắt buộc).</summary>
    public string? RecordReferrerCode { get; set; }
}
