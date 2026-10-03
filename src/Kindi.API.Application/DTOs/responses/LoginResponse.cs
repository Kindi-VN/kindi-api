public class LoginResponse
{
	public string Token { get; set; } = string.Empty;
	public DateTime ExpiresAt { get; set; }
	public Guid Id { get; set; }
	public string Username { get; set; } = string.Empty;
	public string FullName { get; set; } = string.Empty;
	public string Role { get; set; } = string.Empty;

	/// <summary>True khi tài khoản phải đổi tên đăng nhập/mật khẩu trước khi dùng tiếp.</summary>
	public bool MustChangeCredentials { get; set; }

	/// <summary>Mã quyền (P###) của tài khoản — UI dùng để ẩn/hiện menu, nút và chặn route.</summary>
	public IReadOnlyList<string> Permissions { get; set; } = new List<string>();

	/// <summary>Phiên bản quyền lúc cấp token — so với claim <c>permv</c> để biết cần refresh token.</summary>
	public long PermissionsVersion { get; set; }
}