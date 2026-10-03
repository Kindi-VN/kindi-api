using System.Security.Claims;

namespace Kindi.API.Shared.Common.Interfaces;

public interface IJwtService
{
	/// <summary>Cấp token kèm vai trò và mã quyền (P###) của tài khoản.</summary>
	string GenerateToken(string userId, string username, IEnumerable<string> roles,
		IEnumerable<string>? permissions = null, long permissionsVersion = 0);
	ClaimsPrincipal? ValidateToken(string token);
	Task<ClaimsPrincipal?> ValidateTokenWithUserAsync(string token);
	void BlacklistToken(string token);
	bool IsTokenBlacklisted(string token);
	Task<string?> RefreshTokenAsync(string token);
}