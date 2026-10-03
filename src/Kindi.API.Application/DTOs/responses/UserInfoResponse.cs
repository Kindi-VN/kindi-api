// Application/DTOs/responses/UserInfoResponse.cs
namespace Kindi.API.Application.DTOs.responses;

public class UserInfoResponse
{
	public Guid Id { get; set; }
	public string? UserCode { get; set; }
	public string Username { get; set; } = string.Empty;
	public string FullName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string? Phone { get; set; }
	public string? Zalo { get; set; }
	public string Role { get; set; } = string.Empty;
	public bool IsActive { get; set; }
	public bool MustChangeCredentials { get; set; }
	public DateTime? LastLoginAt { get; set; }

	/// <summary>Mã quyền (P###) của tài khoản — UI dùng để ẩn/hiện menu, nút và chặn route.</summary>
	public IReadOnlyList<string> Permissions { get; set; } = new List<string>();

	/// <summary>Phiên bản quyền hiện tại — UI so để quyết định gọi refresh token.</summary>
	public long PermissionsVersion { get; set; }
}