namespace Kindi.API.Domain.Enums;

/// <summary>
/// Nhóm tài khoản của hệ thống. Giá trị số giữ nguyên khi đổi tên để không phải migrate dữ liệu cũ:
/// Customer(1) → <see cref="User"/>, CTV(2) → <see cref="Partner"/> (DB chưa có dòng nào mang giá trị 2).
/// </summary>
public enum UserRole
{
	/// <summary>Khách hàng và cộng tác viên — một nhóm duy nhất.</summary>
	User = 1,

	/// <summary>Đối tác chiến lược (tự động gán khi hồ sơ đối tác được duyệt).</summary>
	Partner = 2,

	/// <summary>Quản trị viên.</summary>
	Admin = 3,

	/// <summary>Quản trị tối cao — duy nhất 1 tài khoản, không có API nào gán được.</summary>
	SuperAdmin = 4
}
