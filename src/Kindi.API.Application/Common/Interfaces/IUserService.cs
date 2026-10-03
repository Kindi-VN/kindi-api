using Kindi.API.Application.Common.Models;
using Kindi.API.Domain.Entities;

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
    /// Không ghi đè SĐT/email của tài khoản đã tồn tại.
    /// </summary>
    Task<PublicUserResult> ResolvePublicUserAsync(string fullName, string phone, string? email, string? zalo);

    /// <summary>
    /// Thông tin cá nhân của người dùng để trả ra DTO (email tạm <c>{sđt}@temp.com</c> trả về null).
    /// </summary>
    Task<UserPersonalInfo?> GetPersonalInfoAsync(Guid userId);

    /// <summary>
    /// Cập nhật thông tin cá nhân vào bảng <c>Users</c> — điểm ghi DUY NHẤT cho mọi luồng form.
    /// <paramref name="allowContactChange"/> = false ở luồng công khai: chỉ điền thêm khi tài khoản còn trống,
    /// không ghi đè SĐT/email của tài khoản đã có (tránh nhập SĐT người khác để sửa hồ sơ của họ).
    /// </summary>
    Task UpdatePersonalInfoAsync(Guid userId, string? fullName, string? phone, string? email, string? zalo, bool allowContactChange = false);
}