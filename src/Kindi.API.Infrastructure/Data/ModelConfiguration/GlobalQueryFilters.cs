using Kindi.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Kindi.API.Infrastructure.Data.ModelConfiguration;

/// <summary>
/// Toàn bộ global query filter của model khai ở MỘT file này.
/// Filter áp theo base class nên entity mới không phải khai lại gì;
/// muốn một truy vấn bỏ qua filter thì dùng IgnoreQueryFilters().
/// </summary>
public static class GlobalQueryFilters
{
    /// <summary>Áp mọi global filter vào model.</summary>
    public static void Apply(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var filter = BuildFilter(entityType.ClrType);
            if (filter != null)
                entityType.SetQueryFilter(filter);
        }
    }

    /// <summary>
    /// Filter của một entity: mọi entity kế thừa <see cref="BaseEntity"/> bị lọc bỏ bản ghi đã xoá mềm
    /// (IsDeleted = true).
    /// Cần thêm filter riêng cho một entity thì thêm nhánh vào switch dưới đây và ghép với filter hiện có
    /// bằng <c>.And(...)</c> (ExpressionExtensions) — vẫn chỉ sửa file này.
    /// </summary>
    private static LambdaExpression? BuildFilter(Type entityClrType)
    {
        if (!typeof(BaseEntity).IsAssignableFrom(entityClrType))
            return null;

        var parameter = Expression.Parameter(entityClrType, "e");
        var notDeleted = Expression.Equal(
            Expression.Property(parameter, nameof(BaseEntity.IsDeleted)),
            Expression.Constant(false));

        return Expression.Lambda(notDeleted, parameter);
    }
}
