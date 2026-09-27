using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Reflection;
using Kindi.API.Application.Common.Configurations;

namespace Kindi.API.Application.Common.Extensions;

/// <summary>
/// Extension methods cho IQueryable — dùng free-style query cầu kỳ.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Áp predicate khi condition đúng (dùng cho filter tùy chọn từ query param).
    /// </summary>
    public static IQueryable<T> WhereIf<T>(this IQueryable<T> source, bool condition, Expression<Func<T, bool>> predicate)
        => condition ? source.Where(predicate) : source;

    /// <summary>
    /// Áp predicate nếu không null (dùng cho expression xây dựng dần).
    /// </summary>
    public static IQueryable<T> WhereIfNotNull<T>(this IQueryable<T> source, Expression<Func<T, bool>>? predicate)
        => predicate == null ? source : source.Where(predicate);

    /// <summary>
    /// Áp predicate khi giá trị filter khác null/default (free-style filter theo DTO).
    /// </summary>
    public static IQueryable<T> WhereIfNotNull<T, TValue>(this IQueryable<T> source, TValue? value, Expression<Func<T, bool>> predicate)
        => value is null ? source : source.Where(predicate);

    /// <summary>
    /// Skip/Take theo trang.
    /// </summary>
    public static IQueryable<T> PageBy<T>(this IQueryable<T> source, int pageNumber, int pageSize)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1;

        return source.Skip((pageNumber - 1) * pageSize).Take(pageSize);
    }

    /// <summary>
    /// Sort động theo chuỗi (vd "CreatedAt" + "desc"). Tên cột được validate qua reflection
    /// để tránh SQL injection / lỗi runtime; nếu không hợp lệ sẽ fallback về defaultSortBy.
    /// </summary>
    public static IQueryable<T> OrderByDynamic<T>(
        this IQueryable<T> source,
        string? sortBy,
        string? sortOrder = "asc",
        string? defaultSortBy = null)
    {
        var direction = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase) ? "descending" : "ascending";
        var column = ResolveSortColumn<T>(sortBy) ?? ResolveSortColumn<T>(defaultSortBy);

        if (string.IsNullOrEmpty(column))
            return source;

        return source.OrderBy($"{column} {direction}");
    }

    /// <summary>
    /// Sort theo expression, kiểu trỏ thẳng (vd: e => e.CreatedAt).
    /// sortOrder: "asc" | "desc".
    /// </summary>
    public static IQueryable<T> SortBy<T, TKey>(
        this IQueryable<T> source,
        Expression<Func<T, TKey>> keySelector,
        string? sortOrder = "asc")
    {
        var useDescending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        return useDescending
            ? source.OrderByDescending(keySelector)
            : source.OrderBy(keySelector);
    }

    /// <summary>
    /// Sort tùy biến theo delegate — kiểu sortOrder truyền thẳng Func<IQueryable, IQueryable>.
    /// VD: query.ApplySort(o => o.OrderByDescending(x => x.CreatedAt));
    /// </summary>
    public static IQueryable<T> ApplySort<T>(
        this IQueryable<T> source,
        Func<IQueryable<T>, IQueryable<T>>? sortFunc)
    {
        return sortFunc == null ? source : sortFunc(source);
    }

    /// <summary>
    /// Left Join cho IQueryable (LINQ không có sẵn).
    /// VD: query.LeftJoin(_queryService.GetQueryable<Partner>(), a => a.UserId, b => b.Id, (a,b) => new {...})
    /// </summary>
    public static IQueryable<TResult> LeftJoin<TOuter, TInner, TKey, TResult>(
        this IQueryable<TOuter> outer,
        IQueryable<TInner> inner,
        Expression<Func<TOuter, TKey>> outerKeySelector,
        Expression<Func<TInner, TKey>> innerKeySelector,
        Expression<Func<TOuter, TInner?, TResult>> resultSelector)
    {
        // Nhóm bên phải theo key; khi không khớp thì Inner = null (Left Join)
        var grouped = outer.GroupJoin(
            inner,
            outerKeySelector,
            innerKeySelector,
            (o, inners) => new LeftJoinRow<TOuter, TInner?>
            {
                Outer = o,
                Inner = inners.FirstOrDefault()
            });

        // Build projection bằng Expression.Invoke để EF Core inline được body của resultSelector
        var xParam = Expression.Parameter(typeof(LeftJoinRow<TOuter, TInner?>), "x");
        var outerAccess = Expression.PropertyOrField(xParam, "Outer");
        var innerAccess = Expression.PropertyOrField(xParam, "Inner");
        var invokeBody = Expression.Invoke(resultSelector, outerAccess, innerAccess);
        var projection = Expression.Lambda<Func<LeftJoinRow<TOuter, TInner?>, TResult>>(
            invokeBody, xParam);

        return grouped.Select(projection);
    }

    /// <summary>
    /// Row trung gian cho LeftJoin — cho phép EF Core dịch sang LEFT JOIN.
    /// </summary>
    private sealed class LeftJoinRow<TOuter, TInner>
    {
        public TOuter? Outer { get; set; }
        public TInner? Inner { get; set; }
    }

    /// <summary>
    /// Lọc "chứa một trong các giá trị" — tương đương TVP bên SQL Server: cả danh sách đi xuống DB
    /// trong MỘT tham số mảng (PostgreSQL sinh ra <c>= ANY(@p)</c>), không sinh mỗi giá trị một tham số.
    /// VD: <c>query.Contains(x => x.Id, groupIds)</c>. Danh sách rỗng ⇒ không bản ghi nào khớp.
    /// </summary>
    public static IQueryable<T> Contains<T, TKey>(
        this IQueryable<T> source,
        Expression<Func<T, TKey>> selector,
        IEnumerable<TKey>? values)
        => source.Contains(selector, values, QueryOptions.Default);

    /// <summary>
    /// Như trên, nhận cấu hình <see cref="QueryOptions"/> (lọc trùng, số giá trị tối đa) —
    /// inject <c>IOptions&lt;QueryOptions&gt;</c> ở service rồi truyền <c>_queryOptions.Value</c>.
    /// </summary>
    public static IQueryable<T> Contains<T, TKey>(
        this IQueryable<T> source,
        Expression<Func<T, TKey>> selector,
        IEnumerable<TKey>? values,
        QueryOptions options)
    {
        var keys = PrepareValues(values, options);

        // Không có giá trị nào thì không bản ghi nào khớp
        if (keys.Count == 0)
            return source.Where(_ => false);

        return source.Where(BuildContainsPredicate(selector, keys));
    }

    /// <summary>
    /// Dựng predicate "thuộc danh sách giá trị". Danh sách được giữ trong một object và truy cập qua
    /// property để EF đưa xuống DB thành MỘT tham số mảng (PostgreSQL: <c>= ANY(@p)</c>), không nhúng
    /// từng giá trị vào câu SQL — nhờ vậy SQL ổn định và tận dụng được kế hoạch thực thi đã cache.
    /// </summary>
    private static Expression<Func<T, bool>> BuildContainsPredicate<T, TKey>(
        Expression<Func<T, TKey>> selector,
        List<TKey> keys)
    {
        var holder = new ContainsValues<TKey>();
        holder.Values.AddRange(keys);

        var valuesAccess = Expression.Property(
            Expression.Constant(holder), nameof(ContainsValues<TKey>.Values));

        var containsCall = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Contains),
            new[] { typeof(TKey) },
            valuesAccess,
            selector.Body);

        return Expression.Lambda<Func<T, bool>>(containsCall, selector.Parameters[0]);
    }

    /// <summary>Giữ danh sách giá trị của điều kiện chứa để EF tham số hoá thay vì nhúng vào SQL.</summary>
    private sealed class ContainsValues<TKey>
    {
        public List<TKey> Values { get; } = new();
    }

    /// <summary>Chuẩn hoá danh sách giá trị trước khi truyền xuống DB (lọc trùng, cắt theo cấu hình).</summary>
    private static List<TKey> PrepareValues<TKey>(IEnumerable<TKey>? values, QueryOptions options)
    {
        if (values == null)
            return new List<TKey>();

        var keys = options.DistinctContainsValues
            ? values.Distinct().ToList()
            : values.ToList();

        if (options.MaxContainsValues > 0 && keys.Count > options.MaxContainsValues)
            keys = keys.Take(options.MaxContainsValues).ToList();

        return keys;
    }

    private static string? ResolveSortColumn<T>(string? sortBy)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
            return null;

        var property = typeof(T).GetProperty(sortBy.Trim(),
            BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        return property?.Name;
    }
}