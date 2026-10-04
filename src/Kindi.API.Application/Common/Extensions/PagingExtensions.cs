using Kindi.API.Application.Common.Models;
using Kindi.API.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Kindi.API.Application.Common.Extensions;

/// <summary>
/// Extension methods phân trang cho IQueryable — trả về PagedList.
/// </summary>
public static class PagingExtensions
{
    /// <summary>
    /// Phân trang đơn giản (count + PageBy + ToListAsync).
    /// </summary>
    public static async Task<PagedList<T>> ToPagedListAsync<T>(
        this IQueryable<T> source,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var count = await source.CountAsync(cancellationToken);
        var items = await source
            .PageBy(pageNumber, pageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<T>(items, count, pageNumber, pageSize);
    }

    /// <summary>
    /// Sort động trước, rồi phân trang — tiện cho query theo query-param.
    /// </summary>
    public static async Task<PagedList<T>> ToPagedListAsync<T>(
        this IQueryable<T> source,
        int pageNumber,
        int pageSize,
        string? sortBy,
        string? sortOrder = null,
        string? defaultSortBy = null,
        CancellationToken cancellationToken = default)
    {
        source = source.OrderByDynamic(sortBy, sortOrder, defaultSortBy);
        return await source.ToPagedListAsync(pageNumber, pageSize, cancellationToken);
    }

    /// <summary>
    /// Phân trang + sort động từ SortableQueryRequest (chuẩn cho DTO kế thừa base).
    /// </summary>
    public static async Task<PagedList<T>> ToPagedListAsync<T>(
        this IQueryable<T> source,
        SortableQueryRequest request,
        string? defaultSortBy = null,
        CancellationToken cancellationToken = default)
    {
        source = source.OrderByDynamic(request.SortBy, request.SortOrder, defaultSortBy);
        return await source.ToPagedListAsync(request.PageNumber, request.PageSize, cancellationToken);
    }

    /// <summary>
    /// Sort tùy biến theo delegate (kiểu sortOrder: o => o.OrderByDescending(x => x.CreatedAt)),
    /// rồi phân trang.
    /// </summary>
    public static async Task<PagedList<T>> ToPagedListAsync<T>(
        this IQueryable<T> source,
        int pageNumber,
        int pageSize,
        Func<IQueryable<T>, IQueryable<T>>? sortFunc,
        CancellationToken cancellationToken = default)
    {
        source = source.ApplySort(sortFunc);
        return await source.ToPagedListAsync(pageNumber, pageSize, cancellationToken);
    }

    /// <summary>
    /// Sort theo expression (kiểu sortBy: e => e.CreatedAt) + sortOrder, rồi phân trang.
    /// </summary>
    public static async Task<PagedList<T>> ToPagedListAsync<T, TKey>(
        this IQueryable<T> source,
        int pageNumber,
        int pageSize,
        Expression<Func<T, TKey>> sortBy,
        string? sortOrder = "asc",
        CancellationToken cancellationToken = default)
    {
        source = source.SortBy(sortBy, sortOrder);
        return await source.ToPagedListAsync(pageNumber, pageSize, cancellationToken);
    }
}