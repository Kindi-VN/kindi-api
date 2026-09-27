using Kindi.API.Application.Common.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Kindi.API.Infrastructure.Data.ModelConfiguration;

/// <summary>
/// Cấu hình model của Kindi ở một chỗ, dùng chung cho cả DbContext ghi
/// (<see cref="ApplicationDbContext"/>) và DbContext chỉ đọc (<see cref="ReadOnlyDbContext"/>).
///
/// Thêm entity mới: chỉ cần khai DbSet trong <see cref="ApplicationDbContext"/> — entity tự vào model
/// và tự được áp global filter. Thêm/sửa global filter: chỉ sửa <see cref="GlobalQueryFilters"/>.
/// </summary>
public static class KindiModelBuilderExtensions
{
    /// <summary>
    /// Đăng ký entity + cấu hình chi tiết theo entity + global query filter cho model.
    /// </summary>
    public static void ConfigureKindiModel(this ModelBuilder modelBuilder)
    {
        // 1) Mọi entity có DbSet trong ApplicationDbContext đều thuộc model.
        //    DbContext chỉ đọc không khai lại DbSet mà vẫn query được các entity này.
        foreach (var entityType in GetDbSetEntityTypes())
            modelBuilder.Entity(entityType);

        // 2) Cấu hình cột/index/relationship của từng entity nằm trong Data/Configurations.
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // 3) Global query filter — khai ở một file duy nhất.
        GlobalQueryFilters.Apply(modelBuilder);

        // 4) Hàm dưới DB dùng trong truy vấn tìm kiếm (bỏ dấu tiếng Việt).
        modelBuilder.HasDbFunction(typeof(KindiDbFunctions).GetMethod(nameof(KindiDbFunctions.Unaccent))!)
            .HasName("unaccent");
    }

    /// <summary>Entity type khai qua các DbSet của <see cref="ApplicationDbContext"/>.</summary>
    private static IEnumerable<Type> GetDbSetEntityTypes()
        => typeof(ApplicationDbContext)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType.IsGenericType
                        && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(p => p.PropertyType.GetGenericArguments()[0])
            .Distinct();
}
