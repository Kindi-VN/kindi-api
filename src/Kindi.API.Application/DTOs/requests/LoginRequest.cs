// LoginRequest.cs
namespace Kindi.API.Application.DTOs.requests;

public class LoginRequest
{
	public string Username { get; set; } = string.Empty;
	public string Password { get; set; } = string.Empty;

	/// <summary>
	/// Ngôn ngữ người dùng đang dùng ở UI (vi | en) — API ghi vào token để mọi response sau đó
	/// được dịch đúng ngôn ngữ này.
	/// </summary>
	public string? Language { get; set; }
}