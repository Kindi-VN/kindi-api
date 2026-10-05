using System.Text.Json.Serialization;

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
