using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Models;
using System.Linq.Expressions;

namespace Kindi.API.Domain.Interfaces;

public interface IRepository<T> where T : class
{
    // ========== QUERY METHODS ==========
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Lấy entity theo Id, BỎ QUA global soft-delete filter (để tìm record đã xóa mềm).</summary>
    Task<T?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);
    Task<T?> GetFirstAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    IQueryable<T> GetQueryable();
    Task<IQueryable<T>> GetQueryableAsync();

    // ========== PAGED METHODS ==========
    Task<PagedList<T>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    Task<PagedList<T>> GetPagedWithOrderAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, object>>? orderBy,
        bool isDescending = true,
        CancellationToken cancellationToken = default);

    Task<PagedList<T>> GetPagedWithIncludesAsync(
        int pageNumber,
        int pageSize,
        Func<IQueryable<T>, IQueryable<T>>? includes = null,
        Expression<Func<T, bool>>? predicate = null,
        Expression<Func<T, object>>? orderBy = null,
        bool isDescending = true,
        CancellationToken cancellationToken = default);

    // ========== ADVANCED QUERY METHODS ==========
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
    Task<T?> GetFirstWithIncludesAsync(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IQueryable<T>>? includes = null,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetListWithIncludesAsync(
        Func<IQueryable<T>, IQueryable<T>>? includes = null,
        Expression<Func<T, bool>>? predicate = null,
        Expression<Func<T, object>>? orderBy = null,
        bool isDescending = true,
        CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> FromSqlRawAsync(string sql, params object[] parameters);
    Task<IEnumerable<T>> GetDeletedAsync(CancellationToken cancellationToken = default);

    // ========== COMMAND METHODS ==========
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    void Update(T entity);
    void UpdateRange(IEnumerable<T> entities);
    void Delete(T entity);
    void DeleteRange(IEnumerable<T> entities);
    void Restore(T entity);
    void RestoreRange(IEnumerable<T> entities);

    // ========== HARD DELETE (xoá vĩnh viễn, chỉ dùng cho màn "Đã xoá") ==========
    /// <summary>
    /// Xoá VĨNH VIỄN một bản ghi khỏi DB (không phải xoá mềm): bản ghi mất hẳn, không thể khôi phục.
    /// Dùng cho thao tác "xoá vĩnh viễn" ở màn xem mục đã xoá.
    /// </summary>
    void HardDelete(T entity);

    /// <summary>Xoá vĩnh viễn nhiều bản ghi (một lần lưu).</summary>
    void HardDeleteRange(IEnumerable<T> entities);

    /// <summary>Xoá vĩnh viễn nhiều bản ghi, trả về số dòng đã xoá.</summary>
    Task<int> HardDeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}