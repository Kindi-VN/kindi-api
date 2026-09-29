namespace Kindi.API.Application.Common.Models;

/// <summary>
/// Thông tin cá nhân của một người dùng — nguồn duy nhất là bảng <c>Users</c>.
/// Các bảng nghiệp vụ chỉ tham chiếu <c>UserId</c> rồi lấy lên khi cần.
/// </summary>
public record UserPersonalInfo(string FullName, string? Phone, string? Email, string? Zalo);
