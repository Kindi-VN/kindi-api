namespace Kindi.API.Application.DTOs.requests;

/// <summary>
/// Sửa thông tin người dùng ở màn quản lý (điểm ghi tập trung): họ tên / SĐT / email / Zalo và
/// trạng thái hoạt động. Thông tin cá nhân luôn nằm ở bảng <c>Users</c>.
/// </summary>
public class UpdateUserInfoRequest
{
    public string FullName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Zalo { get; set; }

    /// <summary>Bật / tắt trạng thái hoạt động của tài khoản; bỏ trống thì giữ nguyên.</summary>
    public bool? IsActive { get; set; }
}
