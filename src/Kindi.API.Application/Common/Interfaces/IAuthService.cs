using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;

namespace Kindi.API.Application.Common.Interfaces;

public interface IAuthService
{
	Task<LoginResponse?> LoginAsync(LoginRequest request);
	Task LogoutAsync(string token);
	Task<LoginResponse?> RefreshTokenAsync(string token);
	Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);

	/// <summary>Đổi tên đăng nhập + mật khẩu, trả token mới (username là claim trong token).</summary>
	Task<LoginResponse?> ChangeCredentialsAsync(Guid userId, ChangeCredentialsRequest request);
	Task ForgotPasswordAsync(ForgotPasswordRequest request);
	/// <summary>Đặt lại mật khẩu cho chính tài khoản đang đăng nhập.</summary>
	Task<bool> ResetPasswordAsync(Guid userId, ResetPasswordRequest request);
	Task<UserInfoResponse?> GetUserByIdAsync(Guid userId);
}