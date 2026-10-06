namespace Kindi.API.UnitTests;

using System.Globalization;
using Kindi.API.Application.Common.Configurations;
using Kindi.API.Application.Resources;
using Kindi.API.Application.Services;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Infrastructure.Data;
using Kindi.API.Infrastructure.Repositories;
using Kindi.API.Shared.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

/// <summary>
/// Gom các test phải đổi culture của tiến trình (culture là trạng thái dùng chung) vào một collection
/// chạy TUẦN TỰ — chạy song song chúng giẫm lên nhau và cho kết quả sai ngẫu nhiên.
/// </summary>
[CollectionDefinition("permission-culture", DisableParallelization = true)]
public class PermissionCultureCollection
{
}

/// <summary>Đặt culture tạm cho một khối test rồi trả lại như cũ.</summary>
internal sealed class CultureScope : IDisposable
{
    private readonly CultureInfo _culture;
    private readonly CultureInfo _uiCulture;

    public CultureScope(string culture)
    {
        _culture = CultureInfo.CurrentCulture;
        _uiCulture = CultureInfo.CurrentUICulture;

        var info = new CultureInfo(culture);
        CultureInfo.CurrentCulture = info;
        CultureInfo.CurrentUICulture = info;
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _culture;
        CultureInfo.CurrentUICulture = _uiCulture;
    }
}

internal static class TestLocalizer
{
    /// <summary>Localizer thật đọc resx nhúng của API — dịch theo CultureInfo.CurrentUICulture tại thời điểm gọi.</summary>
    public static IStringLocalizer<SharedResource> Create()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization();

        var factory = services.BuildServiceProvider().GetRequiredService<IStringLocalizerFactory>();
        return new StringLocalizer<SharedResource>(factory);
    }
}

/// <summary>Dựng PermissionService/JwtService trên DB in-memory cho test.</summary>
internal static class PermissionTestFixture
{
    public static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"kindi-permission-{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options, new StubCurrentUserService());
    }

    public static PermissionService CreateService(ApplicationDbContext context)
        => new(
            new QueryService(context, new ReadContextStub(context)),
            new GenericRepository<RolePermission>(context, new UnitOfWorkStub(context)),
            new GenericRepository<UserPermission>(context, new UnitOfWorkStub(context)),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<PermissionService>.Instance,
            TestLocalizer.Create());

    public static JwtService CreateJwtService(ApplicationDbContext context)
        => new(
            Options.Create(new JwtSettings
            {
                Secret = "kindi-unit-test-secret-key-0123456789-abcdefghij",
                Issuer = "kindi-tests",
                Audience = "kindi-tests",
                ExpiryMinutes = 30
            }),
            new GenericRepository<User>(context, new UnitOfWorkStub(context)),
            NullLogger<JwtService>.Instance);
}

internal sealed class ReadContextStub(ApplicationDbContext context) : IReadDbContext
{
    public DbSet<T> Set<T>() where T : class => context.Set<T>();
}

internal sealed class UnitOfWorkStub(ApplicationDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task CommitTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task RollbackTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}

internal sealed class StubCurrentUserService : ICurrentUserService
{
    public string? UserId => null;
    public string? UserName => "system";
    public bool IsAuthenticated => false;
    public bool IsInRole(string role) => false;
    public string? IpAddress => null;
    public string? UserAgent => null;
}
