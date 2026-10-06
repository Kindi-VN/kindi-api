using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Shared.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Kindi.API.Application.Common.Configurations;

public class JwtService : IJwtService
{
	private readonly JwtSettings _jwtSettings;
	private readonly IRepository<User> _userRepository;
	private readonly ILogger<JwtService> _logger;
	private readonly HashSet<string> _blacklistedTokens = new();

	public JwtService(
		IOptions<JwtSettings> jwtSettings,
		IRepository<User> userRepository,
		ILogger<JwtService> logger)
	{
		_jwtSettings = jwtSettings.Value;
		_userRepository = userRepository;
		_logger = logger;
	}

	public string GenerateToken(string userId, string username, IEnumerable<string> roles,
		IEnumerable<string>? permissions = null, long permissionsVersion = 0, string? language = null)
	{
		var claims = new List<Claim>
		{
			new Claim(ClaimTypes.NameIdentifier, userId),
			new Claim(ClaimTypes.Name, username),
			new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
			new Claim(JwtRegisteredClaimNames.Sub, userId),
			new Claim(JwtRegisteredClaimNames.Email, username)
		};

		claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

		// Quyền nằm ngay trong token (1 claim gộp) — API kiểm quyền không phải truy vấn DB mỗi request.
		var permissionCodes = permissions?.Where(code => !string.IsNullOrWhiteSpace(code)).Distinct().ToList() ?? new List<string>();
		if (permissionCodes.Count > 0)
		{
			claims.Add(new Claim(Kindi.API.Shared.Constants.AuthClaimConstants.Permissions, string.Join(",", permissionCodes)));
		}

		claims.Add(new Claim(Kindi.API.Shared.Constants.AuthClaimConstants.PermissionsVersion,
			permissionsVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)));

		// Ngôn ngữ người dùng chọn lúc đăng nhập → middleware đọc claim này để dịch mọi response sau đó.
		var normalizedLanguage = Kindi.API.Shared.Common.Helpers.LanguageHelper.Normalize(language);
		if (normalizedLanguage != null)
		{
			claims.Add(new Claim(Kindi.API.Shared.Constants.AuthClaimConstants.Language, normalizedLanguage));
		}

		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
		var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

		var token = new JwtSecurityToken(
			issuer: _jwtSettings.Issuer,
			audience: _jwtSettings.Audience,
			claims: claims,
			expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
			signingCredentials: creds);

		var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
		_logger.LogInformation($"Token generated for user: {username}");

		return tokenString;
	}

	public ClaimsPrincipal? ValidateToken(string token)
	{
		if (string.IsNullOrEmpty(token))
		{
			_logger.LogWarning("Empty token provided for validation");
			return null;
		}

		if (IsTokenBlacklisted(token))
		{
			_logger.LogWarning("Token is blacklisted");
			return null;
		}

		var tokenHandler = new JwtSecurityTokenHandler();
		var key = Encoding.UTF8.GetBytes(_jwtSettings.Secret);

		try
		{
			var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
			{
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(key),
				ValidateIssuer = true,
				ValidIssuer = _jwtSettings.Issuer,
				ValidateAudience = true,
				ValidAudience = _jwtSettings.Audience,
				ValidateLifetime = true,
				ClockSkew = TimeSpan.Zero
			}, out _);

			return principal;
		}
		catch (SecurityTokenExpiredException ex)
		{
			_logger.LogWarning($"Token expired: {ex.Message}");
			return null;
		}
		catch (SecurityTokenInvalidSignatureException ex)
		{
			_logger.LogWarning($"Invalid token signature: {ex.Message}");
			return null;
		}
		catch (Exception ex)
		{
			_logger.LogError($"Token validation failed: {ex.Message}");
			return null;
		}
	}

	public async Task<ClaimsPrincipal?> ValidateTokenWithUserAsync(string token)
	{
		var principal = ValidateToken(token);
		if (principal == null) return null;

		var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
		if (userIdClaim == null) return null;

		if (!Guid.TryParse(userIdClaim.Value, out var userId))
		{
			_logger.LogWarning($"Invalid user ID format: {userIdClaim.Value}");
			return null;
		}

		var users = await _userRepository.FindAsync(u => u.Id == userId && !u.IsDeleted);
		var user = users.FirstOrDefault();

		if (user == null)
		{
			_logger.LogWarning($"User not found: {userId}");
			return null;
		}

		if (!user.IsActive)
		{
			_logger.LogWarning($"User is inactive: {userId}");
			return null;
		}

		return principal;
	}

	public void BlacklistToken(string token)
	{
		_blacklistedTokens.Add(token);
		_logger.LogInformation($"Token blacklisted");
	}

	public bool IsTokenBlacklisted(string token)
	{
		return _blacklistedTokens.Contains(token);
	}

	public async Task<string?> RefreshTokenAsync(string token)
	{
		var principal = await ValidateTokenWithUserAsync(token);
		if (principal == null) return null;

		var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		var username = principal.FindFirst(ClaimTypes.Name)?.Value;
		var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value);
		// Giữ nguyên ngôn ngữ người dùng đã chọn khi cấp lại token.
		var language = principal.FindFirst(Kindi.API.Shared.Constants.AuthClaimConstants.Language)?.Value;

		if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(username))
			return null;

		// Blacklist old token
		BlacklistToken(token);

		// Generate new token
		return GenerateToken(userId, username, roles, language: language);
	}
}