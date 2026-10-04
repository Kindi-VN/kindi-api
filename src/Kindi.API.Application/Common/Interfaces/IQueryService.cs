using Kindi.API.Application.Common.Extensions;
using Kindi.API.Domain.Models;
using System.Linq.Expressions;

namespace Kindi.API.Application.Common.Interfaces;

/// <summary>
/// Service cung cấp IQueryable để query DB free-style, kèm các helper thao tác nhanh.
/// </summary>
public interface IQueryService
{
    /// <summary>
    /// Truy vấn CÓ tracking trên DbContext ghi — dùng khi lấy entity ra để sửa rồi lưu.
    /// </summary>
    IQueryable<T> GetQueryable<T>() where T : class;

    /// <summary>
    /// Truy vấn KHÔNG tracking trên DbContext chỉ đọc — mặc định cho danh sách/chi tiết/tra cứu
    /// (có thể chạy trên connection string đọc riêng).
    /// </summary>
    IQueryable<T> GetQueryableNoTracking<T>() where T : class;

    /// <summary>
    /// Alias ngắn: bắt đầu một chuỗi query có tracking.
    /// </summary>
    IQueryable<T> GetAll<T>() where T : class;

    /// <summary>
    /// Alias ngắn: bắt đầu một chuỗi query không tracking (mặc định cho đọc).
    /// </summary>
    IQueryable<T> GetAllNoTracking<T>() where T : class;

    /// <summary>
    /// Lấy theo Id (Guid) — không tracking.
    /// </summary>
    Task<T?> GetByIdAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Lấy danh sách theo predicate (không tracking).
    /// </summary>
    Task<List<T>> GetListAsync<T>(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Lấy phần tử đầu tiên theo predicate (không tracking).
    /// </summary>
    Task<T?> GetFirstOrDefaultAsync<T>(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Lấy phần tử đầu tiên sau khi sort tùy biến (kiểu o => o.OrderByDescending(...)).
    /// </summary>
    Task<T?> GetFirstOrDefaultAsync<T>(
        Expression<Func<T, bool>>? predicate,
        Func<IQueryable<T>, IQueryable<T>>? sortFunc,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Lấy phần tử cuối cùng theo orderBy (descending mặc định).
    /// </summary>
    Task<T?> GetLastOrDefaultAsync<T, TKey>(
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, TKey>> orderBy,
        bool descending = true,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Kiểm tra tồn tại theo predicate.
    /// </summary>
    Task<bool> AnyAsync<T>(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Đếm theo predicate.
    /// </summary>
    Task<int> CountAsync<T>(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Phân trang đơn giản (không tracking).
    /// </summary>
    Task<PagedList<T>> GetPagedListAsync<T>(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Phân trang + sort động theo chuỗi (sortBy/sortOrder từ query-param).
    /// </summary>
    Task<PagedList<T>> GetPagedListAsync<T>(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? predicate,
        string? sortBy,
        string? sortOrder = null,
        string? defaultSortBy = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Phân trang + sort tùy biến theo delegate (o => o.OrderByDescending(...)).
    /// </summary>
    Task<PagedList<T>> GetPagedListAsync<T>(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? predicate,
        Func<IQueryable<T>, IQueryable<T>>? sortFunc,
        CancellationToken cancellationToken = default) where T : class;
}