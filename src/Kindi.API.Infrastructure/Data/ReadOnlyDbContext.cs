using Kindi.API.Domain.Interfaces;
using Kindi.API.Infrastructure.Data.ModelConfiguration;
using Microsoft.EntityFrameworkCore;

namespace Kindi.API.Infrastructure.Data;

/// <summary>
/// DbContext chỉ đọc cho các truy vấn không tracking (danh sách/chi tiết/tra cứu).
///
/// - Dùng CHUNG model với <see cref="ApplicationDbContext"/> (khai ở <see cref="KindiModelBuilderExtensions"/>)
///   nên global query filter (xoá mềm) vẫn áp đúng.
/// - Không có audit / soft-delete khi save; mọi thao tác ghi bị chặn ngay tại đây.
/// - Connection string lấy từ "ConnectionStrings:ReadConnection"; không cấu hình thì dùng chung DB ghi.
/// - Không khai migrations assembly: migration chỉ chạy trên <see cref="ApplicationDbContext"/>.
/// </summary>
public class ReadOnlyDbContext : DbContext, IReadDbContext
{
    public ReadOnlyDbContext(DbContextOptions<ReadOnlyDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureKindiModel();
    }

    /// <summary>Mọi thao tác ghi phải đi qua <see cref="ApplicationDbContext"/>.</summary>
    public override int SaveChanges()
        => throw new InvalidOperationException(
            $"{nameof(ReadOnlyDbContext)} chỉ dùng để đọc; thao tác ghi phải dùng {nameof(ApplicationDbContext)}.");

    /// <inheritdoc cref="SaveChanges()" />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(
            $"{nameof(ReadOnlyDbContext)} chỉ dùng để đọc; thao tác ghi phải dùng {nameof(ApplicationDbContext)}.");
}
