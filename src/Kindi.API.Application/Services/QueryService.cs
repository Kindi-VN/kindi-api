using Kindi.API.Application.Common.Extensions;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Kindi.API.Application.Services;

public class QueryService : IQueryService
{
    /// <summary>DbContext ghi — chỉ dùng cho truy vấn CÓ tracking (đọc để cập nhật).</summary>
    private readonly IApplicationDbContext _context;

    /// <summary>DbContext chỉ đọc — dùng cho mọi truy vấn KHÔNG tracking.</summary>
    private readonly IReadDbContext _readContext;

    public QueryService(IApplicationDbContext context, IReadDbContext readContext)
    {
        _context = context;
        _readContext = readContext;
    }

    public IQueryable<T> GetQueryable<T>() where T : class
        => _context.Set<T>();

    public IQueryable<T> GetQueryableNoTracking<T>() where T : class
        => _readContext.Set<T>().AsNoTracking();

    /// <inheritdoc />
    public IQueryable<T> GetAll<T>() where T : class
        => GetQueryable<T>();

    /// <inheritdoc />
    public IQueryable<T> GetAllNoTracking<T>() where T : class
        => GetQueryableNoTracking<T>();

    public async Task<T?> GetByIdAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : class
    {
        var entityType = typeof(T);
        var idProperty = entityType.GetProperty("Id");

        if (idProperty == null)
            return await GetFirstOrDefaultAsync<T>(null, cancellationToken);

        var parameter = Expression.Parameter(entityType, "e");
        var propertyAccess = Expression.Property(parameter, idProperty);
        var constant = Expression.Constant(id, typeof(Guid));
        var equality = Expression.Equal(propertyAccess, constant);
        var lambda = Expression.Lambda<Func<T, bool>>(equality, parameter);

        return await GetQueryableNoTracking<T>().FirstOrDefaultAsync(lambda, cancellationToken);
    }

    public async Task<List<T>> GetListAsync<T>(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var query = GetQueryableNoTracking<T>();
        if (predicate != null)
            query = query.Where(predicate);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<T?> GetFirstOrDefaultAsync<T>(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var query = GetQueryableNoTracking<T>();
        if (predicate != null)
            query = query.Where(predicate);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<T?> GetFirstOrDefaultAsync<T>(
        Expression<Func<T, bool>>? predicate,
        Func<IQueryable<T>, IQueryable<T>>? sortFunc,
        CancellationToken cancellationToken = default) where T : class
    {
        var query = GetQueryableNoTracking<T>();
        if (predicate != null)
            query = query.Where(predicate);
        query = query.ApplySort(sortFunc);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<T?> GetLastOrDefaultAsync<T, TKey>(
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, TKey>> orderBy,
        bool descending = true,
        CancellationToken cancellationToken = default) where T : class
    {
        var query = GetQueryableNoTracking<T>();
        if (predicate != null)
            query = query.Where(predicate);
        query = descending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> AnyAsync<T>(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var query = GetQueryableNoTracking<T>();
        if (predicate != null)
            query = query.Where(predicate);
        return await query.AnyAsync(cancellationToken);
    }

    public async Task<int> CountAsync<T>(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var query = GetQueryableNoTracking<T>();
        if (predicate != null)
            query = query.Where(predicate);
        return await query.CountAsync(cancellationToken);
    }

    public async Task<PagedList<T>> GetPagedListAsync<T>(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var query = GetQueryableNoTracking<T>();
        if (predicate != null)
            query = query.Where(predicate);
        return await query.ToPagedListAsync(pageNumber, pageSize, cancellationToken);
    }

    public async Task<PagedList<T>> GetPagedListAsync<T>(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? predicate,
        string? sortBy,
        string? sortOrder = null,
        string? defaultSortBy = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var query = GetQueryableNoTracking<T>();
        if (predicate != null)
            query = query.Where(predicate);
        return await query.ToPagedListAsync(pageNumber, pageSize, sortBy, sortOrder, defaultSortBy, cancellationToken);
    }

    public async Task<PagedList<T>> GetPagedListAsync<T>(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? predicate,
        Func<IQueryable<T>, IQueryable<T>>? sortFunc,
        CancellationToken cancellationToken = default) where T : class
    {
        var query = GetQueryableNoTracking<T>();
        if (predicate != null)
            query = query.Where(predicate);
        return await query.ToPagedListAsync(pageNumber, pageSize, sortFunc, cancellationToken);
    }
}