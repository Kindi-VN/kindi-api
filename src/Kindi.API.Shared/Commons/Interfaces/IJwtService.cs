using System.Security.Claims;

namespace Kindi.API.Shared.Common.Interfaces;

public interface IJwtService
{
	/// <summary>Cấp token kèm vai trò, mã quyền (P###) và ngôn ngữ của tài khoản.</summary>
	string GenerateToken(string userId, string username, IEnumerable<string> roles,
		IEnumerable<string>? permissions = null, long permissionsVersion = 0, string? language = null);
	ClaimsPrincipal? ValidateToken(string token);
	Task<ClaimsPrincipal?> ValidateTokenWithUserAsync(string token);
	void BlacklistToken(string token);
	bool IsTokenBlacklisted(string token);
	Task<string?> RefreshTokenAsync(string token);
}