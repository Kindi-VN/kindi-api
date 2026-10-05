using System.Text.Json.Serialization;

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

    /// <summary>
    /// Nhịp chuyển tiếp: tên cũ "referralCode" của <see cref="RecordReferrerCode"/>.
    /// Chỉ để đọc dữ liệu client cũ; điền vào trường chính khi trường chính còn trống,
    /// còn trường chính đã có giá trị thì bỏ qua tên cũ (tên chính luôn được ưu tiên).
    /// </summary>
    [JsonPropertyName("referralCode")]
    public string? LegacyReferralCode
    {
        set
        {
            if (string.IsNullOrWhiteSpace(RecordReferrerCode))
                RecordReferrerCode = value;
        }
    }
}
