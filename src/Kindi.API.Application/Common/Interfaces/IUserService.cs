using Kindi.API.Application.Common.Models;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Models;

namespace Kindi.API.Application.Common.Interfaces;

public interface IUserService
{
    Task<Guid> GetOrCreateUserAsync(string fullName, string phone, string? email = null);
    Task<User?> GetCurrentUserAsync();

    /// <summary>
    /// Tìm user theo SĐT hoặc email (không tạo mới). Dùng để biết form công khai
    /// có tạo tài khoản mới hay dùng lại tài khoản đã có.
    /// </summary>
    Task<User?> FindByPhoneOrEmailAsync(string? phone, string? email);

    /// <summary>Tìm user theo Id (chưa xoá).</summary>
    Task<User?> FindByIdAsync(Guid userId);

    /// <summary>
    /// Xử lý tài khoản cho luồng công khai: dùng lại tài khoản theo SĐT/email nếu đã có,
    /// tạo tài khoản tự động nếu chưa, rồi cập nhật thông tin cá nhân vào bảng <c>Users</c>.
    /// Không ghi đè họ tên/SĐT/email/Zalo của tài khoản đã tồn tại.
    /// </summary>
    Task<PublicUserResult> ResolvePublicUserAsync(string fullName, string phone, string? email, string? zalo);

    /// <summary>
    /// Thông tin cá nhân của người dùng để trả ra DTO (email tạm <c>{sđt}@temp.com</c> trả về null).
    /// </summary>
    Task<UserPersonalInfo?> GetPersonalInfoAsync(Guid userId);

    /// <summary>
    /// Cập nhật thông tin cá nhân vào bảng <c>Users</c> — điểm ghi DUY NHẤT cho mọi luồng form.
    /// <paramref name="allowContactChange"/> = false ở luồng công khai: chỉ điền thêm khi hồ sơ còn trống,
    /// không ghi đè họ tên/SĐT/email/Zalo của tài khoản đã có (tránh form ghi đè hồ sơ người khác,
    /// kể cả khi người gửi đang đăng nhập). Đổi hồ sơ phải truyền true (chủ hồ sơ qua <c>PUT /Auth/me</c>
    /// hoặc endpoint quản trị).
    /// </summary>
    Task UpdatePersonalInfoAsync(Guid userId, string? fullName, string? phone, string? email, string? zalo, bool allowContactChange = false);

    /// <summary>Danh sách người dùng phân trang (màn quản trị).</summary>
    Task<PagedList<UserInfoResponse>> GetPagedAsync(UserQueryDto query);

    /// <summary>
    /// Cấp lại mật khẩu cho người dùng: mật khẩu mới là chính số điện thoại của tài khoản
    /// và bắt buộc đổi ở lần đăng nhập kế tiếp. Trả về null nếu không tìm thấy tài khoản.
    /// </summary>
    Task<UserInfoResponse?> ResetPasswordToPhoneAsync(Guid userId);
}