using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Infrastructure.Data;
using Kindi.API.Infrastructure.Repositories;
using Kindi.API.Infrastructure.Services;
using Kindi.API.Shared.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kindi.API.Infrastructure;

public static class DependencyInjection
{
	public static IServiceCollection AddInfrastructureServices(
		this IServiceCollection services,
		IConfiguration configuration)
	{
		// Database context (ghi): tracking + audit + soft delete, chạy migration
		services.AddDbContext<ApplicationDbContext>(options =>
			options.UseNpgsql(
				configuration.GetConnectionString("DefaultConnection"),
				b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

		services.AddScoped<IApplicationDbContext>(provider =>
			provider.GetRequiredService<ApplicationDbContext>());

		// Database context (chỉ đọc): dùng cho mọi truy vấn no-tracking.
		// Có "ConnectionStrings:ReadConnection" thì trỏ sang replica đọc, không thì dùng chung DB ghi.
		var readConnectionString = configuration.GetConnectionString("ReadConnection");
		if (string.IsNullOrWhiteSpace(readConnectionString))
			readConnectionString = configuration.GetConnectionString("DefaultConnection");

		services.AddDbContext<ReadOnlyDbContext>(options => options.UseNpgsql(readConnectionString));

		services.AddScoped<IReadDbContext>(provider =>
			provider.GetRequiredService<ReadOnlyDbContext>());

		// Generic repository
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));

        // Current User Service
        services.AddScoped<ICurrentUserService, CurrentUserService>();

		// HttpContextAccessor
		services.AddHttpContextAccessor();

		services.AddScoped<IExcelService, ExcelService>();

		return services;
	}
}
