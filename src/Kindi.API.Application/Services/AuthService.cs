using Kindi.API.Application.Common.Configurations;
using Kindi.API.Application.Common.Exceptions;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.Resources;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Shared.Common.Helpers;
using Kindi.API.Shared.Exceptions;
using Kindi.API.Shared.Common.Interfaces;
using Kindi.API.Shared.Constants;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Kindi.API.Application.Services;

public class AuthService : IAuthService
{
	private readonly IRepository<User> _userRepository;
	private readonly IJwtService _jwtService;
	private readonly JwtSettings _jwtSettings;
	private readonly ILogger<AuthService> _logger;
	private readonly IAuthAuditService _authAuditService;
	private readonly IStringLocalizer<SharedResource> _localizer;

	public AuthService(
		IRepository<User> userRepository,
		IJwtService jwtService,
		IOptions<JwtSettings> jwtSettings,
		ILogger<AuthService> logger,
		IAuthAuditService authAuditService,
		IStringLocalizer<SharedResource> localizer)
	{
		_userRepository = userRepository;
		_jwtService = jwtService;
		_jwtSettings = jwtSettings.Value;
		_logger = logger;
		_authAuditService = authAuditService;
		_localizer = localizer;
	}

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        // Tìm user theo tên đăng nhập, email hoặc số điện thoại; SĐT nhập có thể kèm khoảng trắng,
        // dấu chấm hoặc tiền tố +84 nên so cả dạng đã chuẩn hoá.
        var identifier = request.Username?.Trim() ?? string.Empty;
        var phoneCandidates = PhoneHelper.Candidates(identifier);

        var users = await _userRepository.FindAsync(u =>
            !u.IsDeleted && (
                u.Username == identifier ||
                u.Email == identifier ||
                phoneCandidates.Contains(u.Phone ?? string.Empty)
            )
        );
        var user = users.FirstOrDefault();

        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning($"Login failed for user: {request.Username}");
            await _authAuditService.LogAsync(null, request.Username, AuditAction.Login, false,
                "Sai tên đăng nhập hoặc mật khẩu");
            return null;
        }

        if (!user.IsActive)
        {
            _logger.LogWarning($"Inactive user attempted login: {request.Username}");
            await _authAuditService.LogAsync(user.Id, user.Username, AuditAction.Login, false,
                "Tài khoản đã bị vô hiệu hóa");
            return null;
        }

        var roles = GetRoles(user.Role);
        var token = _jwtService.GenerateToken(user.Id.ToString(), user.Username, roles);

        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync();

        _logger.LogInformation($"User logged in successfully: {user.Username} (ID: {user.Id})");
        await _authAuditService.LogAsync(user.Id, user.Username, AuditAction.Login, true);

        return new LoginResponse
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            MustChangeCredentials = user.MustChangeCredentials
        };
    }

    public async Task LogoutAsync(string token)
	{
		var principal = _jwtService.ValidateToken(token);
		var username = principal?.FindFirst(ClaimTypes.Name)?.Value;
		var userId = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

		_jwtService.BlacklistToken(token);
		_logger.LogInformation("User logged out");
		await _authAuditService.LogAsync(
			Guid.TryParse(userId, out var id) ? id : null,
			username,
			AuditAction.Logout,
			true);
	}

	public async Task<LoginResponse?> RefreshTokenAsync(string token)
	{
		// Lấy thông tin user từ token cũ (trước khi refresh) để ghi audit
		var oldPrincipal = _jwtService.ValidateToken(token);
		var oldUsername = oldPrincipal?.FindFirst(ClaimTypes.Name)?.Value;
		var oldUserId = oldPrincipal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

		var newToken = await _jwtService.RefreshTokenAsync(token);
		if (string.IsNullOrEmpty(newToken))
		{
			_logger.LogWarning("Refresh token failed");
			await _authAuditService.LogAsync(
				Guid.TryParse(oldUserId, out var oldId) ? oldId : null,
				oldUsername,
				AuditAction.RefreshToken,
				false, "Token hết hạn hoặc không hợp lệ");
			return null;
		}

		// Extract user info from new token
		var principal = _jwtService.ValidateToken(newToken);
		var username = principal?.FindFirst(ClaimTypes.Name)?.Value;
		var userId = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

		if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(userId))
		{
			await _authAuditService.LogAsync(null, username ?? oldUsername, AuditAction.RefreshToken, false);
			return null;
		}

		var users = await _userRepository.FindAsync(u => u.Id == Guid.Parse(userId) && !u.IsDeleted);
		var user = users.FirstOrDefault();

		await _authAuditService.LogAsync(
			Guid.TryParse(userId, out var id) ? id : null,
			username,
			AuditAction.RefreshToken,
			true);

		return new LoginResponse
		{
			Token = newToken,
			ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
			Id = user?.Id ?? Guid.Empty,
			Username = username,
			FullName = user?.FullName ?? string.Empty,
			Role = user?.Role.ToString() ?? string.Empty,
			MustChangeCredentials = user?.MustChangeCredentials ?? false
		};
	}

	/// <summary>
	/// Đổi tên đăng nhập + mật khẩu. Dùng cho lần đăng nhập đầu tiên của tài khoản tạo tự động
	/// (bắt buộc đổi) và cho chức năng đổi thông tin đăng nhập ở trang người dùng.
	/// Trả về token mới vì username là claim trong token.
	/// </summary>
	public async Task<LoginResponse?> ChangeCredentialsAsync(Guid userId, ChangeCredentialsRequest request)
	{
		if (request.NewPassword != request.ConfirmNewPassword)
		{
			throw new BadRequestException(_localizer["Collaborator_PasswordMismatch"]);
		}

		var users = await _userRepository.FindAsync(u => u.Id == userId && !u.IsDeleted);
		var user = users.FirstOrDefault();

		if (user == null)
		{
			_logger.LogWarning("User not found: {UserId}", userId);
			await _authAuditService.LogAsync(userId, null, AuditAction.ChangePassword, false,
				"Không tìm thấy người dùng");
			throw new NotFoundException(_localizer["UserNotFound"]);
		}

		if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
		{
			_logger.LogWarning("Invalid current password for user: {UserId}", userId);
			await _authAuditService.LogAsync(userId, user.Username, AuditAction.ChangePassword, false,
				"Mật khẩu hiện tại không đúng");
			throw new BadRequestException(_localizer["CurrentPasswordIncorrect"]);
		}

		var newUsername = request.NewUsername.Trim();

		// Đăng nhập chấp nhận cả Username / Email / Phone → tên đăng nhập mới không được trùng bất kỳ giá trị nào
		var taken = await _userRepository.GetFirstAsync(u =>
			u.Id != userId && !u.IsDeleted &&
			(u.Username == newUsername || u.Email == newUsername || u.Phone == newUsername));

		if (taken != null)
		{
			_logger.LogWarning("Username already taken: {Username}", newUsername);
			await _authAuditService.LogAsync(userId, user.Username, AuditAction.ChangePassword, false,
				$"Tên đăng nhập {newUsername} đã được sử dụng");
			throw new BadRequestException(_localizer["UsernameAlreadyExists"]);
		}

		var oldUsername = user.Username;
		user.Username = newUsername;
		user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
		user.MustChangeCredentials = false;
		_userRepository.Update(user);
		await _userRepository.SaveChangesAsync();

		_logger.LogInformation("Credentials changed for user: {UserId}", userId);
		await _authAuditService.LogAsync(userId, user.Username, AuditAction.ChangePassword, true,
			$"Đổi tên đăng nhập ({oldUsername} → {user.Username}) và mật khẩu");

		return new LoginResponse
		{
			Token = _jwtService.GenerateToken(user.Id.ToString(), user.Username, GetRoles(user.Role)),
			ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
			Username = user.Username,
			FullName = user.FullName,
			Role = user.Role.ToString(),
			MustChangeCredentials = false
		};
	}

	public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
	{
		if (request.NewPassword != request.ConfirmNewPassword)
		{
			_logger.LogWarning("Password confirmation mismatch");
			await _authAuditService.LogAsync(userId, null, AuditAction.ChangePassword, false,
				"Mật khẩu xác nhận không khớp");
			return false;
		}

		var users = await _userRepository.FindAsync(u => u.Id == userId && !u.IsDeleted);
		var user = users.FirstOrDefault();

		if (user == null)
		{
			_logger.LogWarning($"User not found: {userId}");
			await _authAuditService.LogAsync(userId, null, AuditAction.ChangePassword, false,
				"Không tìm thấy người dùng");
			return false;
		}

		if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
		{
			_logger.LogWarning($"Invalid current password for user: {userId}");
			await _authAuditService.LogAsync(userId, user.Username, AuditAction.ChangePassword, false,
				"Mật khẩu hiện tại không đúng");
			return false;
		}

		user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
		// Đã tự đặt mật khẩu mới thì không cần buộc đổi lại ở lần đăng nhập sau.
		user.MustChangeCredentials = false;
		await _userRepository.SaveChangesAsync();

		_logger.LogInformation($"Password changed for user: {userId}");
		await _authAuditService.LogAsync(userId, user.Username, AuditAction.ChangePassword, true);
		return true;
	}

	public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
	{
		// TODO: Send reset password email with token
		_logger.LogInformation($"Password reset requested for email: {request.Email}");
		await _authAuditService.LogAsync(null, request.Email, AuditAction.ResetPassword, true,
			"Yêu cầu gửi link đặt lại mật khẩu");
	}

	/// <summary>
	/// Đặt lại mật khẩu của chính tài khoản đang đăng nhập — danh tính lấy từ token,
	/// không nhận email từ client nên không thể đổi mật khẩu của tài khoản khác.
	/// </summary>
	public async Task<bool> ResetPasswordAsync(Guid userId, ResetPasswordRequest request)
	{
		if (request.NewPassword != request.ConfirmPassword)
		{
			_logger.LogWarning("Password confirmation mismatch");
			await _authAuditService.LogAsync(userId, null, AuditAction.ResetPassword, false,
				"Mật khẩu xác nhận không khớp");
			return false;
		}

		var user = await _userRepository.GetFirstAsync(u => u.Id == userId && !u.IsDeleted);
		if (user == null)
		{
			_logger.LogWarning($"User not found for password reset: {userId}");
			await _authAuditService.LogAsync(userId, null, AuditAction.ResetPassword, false,
				"Không tìm thấy người dùng");
			return false;
		}

		// Client có gửi email thì phải khớp tài khoản đang đăng nhập (tương thích request cũ).
		if (!string.IsNullOrWhiteSpace(request.Email)
			&& !string.Equals(user.Email?.Trim(), request.Email.Trim(), StringComparison.OrdinalIgnoreCase))
		{
			_logger.LogWarning($"Password reset email mismatch for user: {user.Username}");
			await _authAuditService.LogAsync(user.Id, user.Username, AuditAction.ResetPassword, false,
				"Email không khớp tài khoản đang đăng nhập");
			return false;
		}

		user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
		// Đã tự đặt mật khẩu mới thì không cần buộc đổi lại ở lần đăng nhập sau.
		user.MustChangeCredentials = false;
		await _userRepository.SaveChangesAsync();

		_logger.LogInformation($"Password reset for user: {user.Username}");
		await _authAuditService.LogAsync(user.Id, user.Username, AuditAction.ResetPassword, true);
		return true;
	}

	public async Task<UserInfoResponse?> GetUserByIdAsync(Guid userId)
	{
		var users = await _userRepository.FindAsync(u => u.Id == userId && !u.IsDeleted);
		var user = users.FirstOrDefault();

		if (user == null) return null;

		return new UserInfoResponse
		{
			Id = user.Id,
			UserCode = user.UserCode,
			Username = user.Username,
			FullName = user.FullName,
			Email = user.Email,
			Phone = user.Phone,
			Role = user.Role.ToString(),
			IsActive = user.IsActive,
			MustChangeCredentials = user.MustChangeCredentials,
			LastLoginAt = user.LastLoginAt
		};
	}

	/// <summary>
	/// Kiểm tra mật khẩu: thử dạng nhập nguyên văn, sau đó tới dạng SĐT đã chuẩn hoá
	/// (bỏ khoảng trắng/dấu, +84 → 0) để tài khoản có mật khẩu là SĐT không lệch định dạng.
	/// </summary>
	private static bool VerifyPassword(string password, string? passwordHash)
	{
		var hash = passwordHash ?? string.Empty;
		if (PasswordHasher.Verify(password, hash))
			return true;

		var normalized = PhoneHelper.Normalize(password);
		return normalized.Length > 0 && normalized != password && PasswordHasher.Verify(normalized, hash);
	}

	private static List<string> GetRoles(UserRole role)
	{
		// Claim vai trò giữ dạng số để khớp RoleConstants và [Authorize(Roles = ...)].
		return role switch
		{
			UserRole.Admin => new List<string> { RoleConstants.Admin },
			UserRole.CTV => new List<string> { RoleConstants.CTV },
			_ => new List<string> { RoleConstants.Customer }
		};
	}
}