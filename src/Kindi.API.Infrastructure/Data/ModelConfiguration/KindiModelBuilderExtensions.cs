using Kindi.API.Application.Common.Extensions;
using Kindi.API.Application.Common.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq.Expressions;
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

        // 5) Dịch StringQueryExtensions.Like thành unaccent + ILIKE của PostgreSQL để chỗ gọi chỉ cần viết
        //    x.ProductName.Like(keyword) thay cho EF.Functions.ILike(KindiDbFunctions.Unaccent(...), ...).
        modelBuilder.HasDbFunction(typeof(StringQueryExtensions).GetMethod(nameof(StringQueryExtensions.Like),
                new[] { typeof(string), typeof(string) })!)
            .HasTranslation(args => BuildLikeExpression(args));

        // 6) EqualsCode: mã so khớp ĐÚNG bằng dấu = (cột giữ nguyên nên index còn dùng được).
        modelBuilder.HasDbFunction(typeof(StringQueryExtensions).GetMethod(nameof(StringQueryExtensions.EqualsCode),
                new[] { typeof(string), typeof(string) })!)
            .HasTranslation(args => BuildEqualsCodeExpression(args));
    }

    /// <summary>
    /// <c>cột = upper(trim(từ khoá))</c> — chỉ chuẩn hoá THAM SỐ, không bọc hàm quanh cột nên index vẫn dùng được.
    /// </summary>
    private static SqlExpression BuildEqualsCodeExpression(IReadOnlyList<SqlExpression> args)
    {
        var mapping = args[0].TypeMapping ?? new StringTypeMapping("text", System.Data.DbType.String, unicode: false);

        var keyword = Upper(Trim(args[1], mapping), mapping);
        return new SqlBinaryExpression(ExpressionType.Equal, args[0], keyword, typeof(bool), mapping);
    }

    private static SqlExpression Trim(SqlExpression value, RelationalTypeMapping mapping)
        => new SqlFunctionExpression("trim", new[] { value },
            nullable: true, argumentsPropagateNullability: new[] { true }, type: typeof(string), typeMapping: mapping);

    private static SqlExpression Upper(SqlExpression value, RelationalTypeMapping mapping)
        => new SqlFunctionExpression("upper", new[] { value },
            nullable: true, argumentsPropagateNullability: new[] { true }, type: typeof(string), typeMapping: mapping);

    /// <summary>
    /// <c>unaccent(cột) ILIKE '%' || unaccent(từ khoá) || '%'</c> — bỏ dấu cả hai vế nên gõ có dấu hay
    /// không dấu đều tìm ra, không phân biệt hoa/thường (ILIKE). <c>%</c>/<c>_</c> trong từ khoá vẫn là
    /// ký tự đại diện như hành vi cũ của các truy vấn tìm kiếm.
    /// </summary>
    private static SqlExpression BuildLikeExpression(IReadOnlyList<SqlExpression> args)
    {
        var mapping = args[0].TypeMapping ?? new StringTypeMapping("text", System.Data.DbType.String, unicode: false);

        // strpos(lower(unaccent(<cột>)), lower(unaccent(<từ khoá>))) > 0
        // = tìm chứa, bỏ dấu tiếng Việt, không phân biệt hoa/thường (giống ILIKE nhưng không cần dựng node LIKE).
        var column = Lower(Unaccent(args[0], mapping), mapping);
        var keyword = Lower(Unaccent(args[1], mapping), mapping);

        var position = new SqlFunctionExpression("strpos", new SqlExpression[] { column, keyword },
            nullable: true, argumentsPropagateNullability: new[] { true, true }, type: typeof(int), typeMapping: new IntTypeMapping("integer"));

        return new SqlBinaryExpression(ExpressionType.GreaterThan, position,
            new SqlConstantExpression(0, new IntTypeMapping("integer")), typeof(bool), mapping);
    }

    private static SqlExpression Unaccent(SqlExpression value, RelationalTypeMapping mapping)
        => new SqlFunctionExpression("unaccent", new[] { value },
            nullable: true, argumentsPropagateNullability: new[] { true }, type: typeof(string), typeMapping: mapping);

    private static SqlExpression Lower(SqlExpression value, RelationalTypeMapping mapping)
        => new SqlFunctionExpression("lower", new[] { value },
            nullable: true, argumentsPropagateNullability: new[] { true }, type: typeof(string), typeMapping: mapping);

    /// <summary>Entity type khai qua các DbSet của <see cref="ApplicationDbContext"/>.</summary>
    private static IEnumerable<Type> GetDbSetEntityTypes()
        => typeof(ApplicationDbContext)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType.IsGenericType
                        && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(p => p.PropertyType.GetGenericArguments()[0])
            .Distinct();
}
