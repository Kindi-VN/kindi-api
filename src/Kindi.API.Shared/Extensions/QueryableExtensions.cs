using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Kindi.API.Shared.Extensions;

public static class QueryableExtensions
{
    /// <summary>
    /// Include nhiều navigation properties cùng lúc
    /// </summary>
    public static IQueryable<T> IncludeMultiple<T>(
        this IQueryable<T> query,
        params Expression<Func<T, object?>>[] includes)
        where T : class
    {
        if (includes == null || includes.Length == 0)
            return query;

        var result = query;
        foreach (var include in includes)
        {
            result = result.Include(include);
        }
        return result;
    }

    /// <summary>
    /// Include + ThenInclude với cú pháp ngắn, dùng cho navigation dạng collection
    /// (vd <c>query.IncludeThen(p => p.PostTags, pt => pt.Tag)</c>).
    /// </summary>
    public static IIncludableQueryable<T, TThen> IncludeThen<T, TProperty, TThen>(
        this IQueryable<T> query,
        Expression<Func<T, IEnumerable<TProperty>>> include,
        Expression<Func<TProperty, TThen>> thenInclude)
        where T : class
    {
        return query.Include(include).ThenInclude(thenInclude);
    }
}