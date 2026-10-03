namespace Kindi.API.Infrastructure.Data;

using Kindi.API.Application.Common.Helpers;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Rules;
using Kindi.API.Shared.Common.Helpers;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Seed dữ liệu tối thiểu. Nguyên tắc: seeder KHÔNG bao giờ tạo tài khoản SuperAdmin
/// (tài khoản quản trị tối cao cấp phát thủ công, mỗi DB chỉ 1 — xem unique index
/// <c>IX_Users_SuperAdmin_Unique</c>), và không dùng mật khẩu mặc định hard-code trong repo.
/// </summary>
public static class DatabaseSeeder
{
	private const string BootstrapUsernameVariable = "BOOTSTRAP_ADMIN_USERNAME";
	private const string BootstrapPasswordVariable = "BOOTSTRAP_ADMIN_PASSWORD";

	public static async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
	{
		// Đã có tài khoản quản trị (Admin) thì không seed gì thêm.
		if (await context.Users.AnyAsync(u => u.Role == UserRole.Admin, cancellationToken))
		{
			return;
		}

		// Tài khoản quản trị đầu tiên lấy từ biến môi trường của máy chủ; không cấu hình thì bỏ qua
		// (không tạo tài khoản với mật khẩu mặc định nào nằm trong repo).
		var username = Environment.GetEnvironmentVariable(BootstrapUsernameVariable);
		var password = Environment.GetEnvironmentVariable(BootstrapPasswordVariable);
		if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
		{
			return;
		}

		// Chỉ Admin nằm trong danh sách được gán — SuperAdmin không thể sinh ra ở đây.
		var role = UserRole.Admin;
		if (!RoleRules.IsAssignable(role))
		{
			return;
		}

		var admin = new User
		{
			UserCode = CodeGenerator.Generate("USR"),
			Username = username.Trim(),
			PasswordHash = PasswordHasher.Hash(password),
			FullName = "Administrator",
			Email = $"{username.Trim()}@kindi.com",
			IsActive = true,
			MustChangeCredentials = true,
			Role = role
		};

		context.Users.Add(admin);
		await context.SaveChangesAsync(cancellationToken);
	}
}
