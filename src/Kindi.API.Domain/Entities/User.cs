using Kindi.API.Domain.Attributes;
using Kindi.API.Domain.Enums;

namespace Kindi.API.Domain.Entities;

public class User : BaseEntity
{
	public string? UserCode { get; set; }
	public string Username { get; set; } = string.Empty;
	[AuditIgnore]
	public string? PasswordHash { get; set; } // Cho phép null
	public string FullName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string? Phone { get; set; }

	/// <summary>Số Zalo liên hệ (khác SĐT nếu người dùng nhập khác).</summary>
	public string? Zalo { get; set; }
	public bool IsActive { get; set; } = true;

	/// <summary>
	/// Bắt buộc đổi tên đăng nhập + mật khẩu ngay lần đăng nhập đầu tiên.
	/// Đặt true cho tài khoản tạo tự động từ form công khai (username user&lt;sđt&gt;, mật khẩu = SĐT).
	/// </summary>
	public bool MustChangeCredentials { get; set; } = false;
	public DateTime? LastLoginAt { get; set; }
	public UserRole Role { get; set; } = UserRole.User;

	/// <summary>Mã chia sẻ riêng của tài khoản (gắn vào link chia sẻ; CTV dùng mã trên hồ sơ CTV).</summary>
	public string? ReferralCode { get; set; }

	/// <summary>
	/// Mã chia sẻ của người đã mang tài khoản này tới hệ thống (lấy từ link chia sẻ ?ref=).
	/// Chỉ ghi nhận LẦN ĐẦU — mở link của CTV khác sau đó không ghi đè.
	/// </summary>
	public string? ReferredByCode { get; set; }

	/// <summary>Thời điểm ghi nhận <see cref="ReferredByCode"/>.</summary>
	public DateTime? ReferredAt { get; set; }
}