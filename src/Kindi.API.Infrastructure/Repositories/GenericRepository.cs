using Kindi.API.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Domain.Entities;

namespace Kindi.API.Infrastructure.Repositories;

/// <summary>
/// Generic Repository với Auto-Save - Tự động lưu khi gọi Add/Update/Delete
/// </summary>
public class GenericRepository<T> : IRepository<T> where T : class
{
    protected readonly IApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;
    protected readonly IUnitOfWork _unitOfWork;

    public GenericRepository(IApplicationDbContext context, IUnitOfWork unitOfWork)
    {
        _context = context;
        _dbSet = context.Set<T>();
        _unitOfWork = unitOfWork;
    }

    // ========== QUERY METHODS ==========
    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet.FindAsync(new object[] { id }, cancellationToken);

    /// <summary>
    /// Lấy entity theo Id, BỎ QUA global soft-delete filter (để tìm record đã xóa mềm khi restore).
    /// </summary>
    public async Task<T?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);

    public async Task<T?> GetFirstAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await _dbSet.FirstOrDefaultAsync(predicate, cancellationToken);

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbSet.ToListAsync(cancellationToken);

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await _dbSet.Where(predicate).ToListAsync(cancellationToken);

    // ========== PAGED METHODS ==========
    public async Task<PagedList<T>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = _dbSet;
        if (predicate != null) query = query.Where(predicate);
        return await PagedList<T>.CreateAsync(query, pageNumber, pageSize);
    }

    public async Task<PagedList<T>> GetPagedWithOrderAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, object>>? orderBy,
        bool isDescending = true,
        CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = _dbSet;
        if (predicate != null) query = query.Where(predicate);
        if (orderBy != null)
            query = isDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
        return await PagedList<T>.CreateAsync(query, pageNumber, pageSize);
    }

    public async Task<PagedList<T>> GetPagedWithIncludesAsync(
        int pageNumber,
        int pageSize,
        Func<IQueryable<T>, IQueryable<T>>? includes = null,
        Expression<Func<T, bool>>? predicate = null,
        Expression<Func<T, object>>? orderBy = null,
        bool isDescending = true,
        CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = _dbSet;
        // Lọc trước rồi mới join/include: câu SQL chỉ join trên tập đã thu hẹp
        if (predicate != null) query = query.Where(predicate);
        if (includes != null) query = includes(query);
        if (orderBy != null)
            query = isDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
        return await PagedList<T>.CreateAsync(query, pageNumber, pageSize);
    }

    // ========== ADVANCED QUERY METHODS ==========
    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = _dbSet;
        if (predicate != null) query = query.Where(predicate);
        return await query.CountAsync(cancellationToken);
    }

    public async Task<T?> GetFirstWithIncludesAsync(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IQueryable<T>>? includes = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = _dbSet;
        // Lọc trước rồi mới join/include: câu SQL chỉ join trên tập đã thu hẹp
        query = query.Where(predicate);
        if (includes != null) query = includes(query);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<T>> GetListWithIncludesAsync(
        Func<IQueryable<T>, IQueryable<T>>? includes = null,
        Expression<Func<T, bool>>? predicate = null,
        Expression<Func<T, object>>? orderBy = null,
        bool isDescending = true,
        CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = _dbSet;
        if (predicate != null) query = query.Where(predicate);
        if (includes != null) query = includes(query);
        if (orderBy != null)
            query = isDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await _dbSet.AnyAsync(predicate, cancellationToken);

    public async Task<IEnumerable<T>> FromSqlRawAsync(string sql, params object[] parameters)
        => await _dbSet.FromSqlRaw(sql, parameters).ToListAsync();

    public async Task<IEnumerable<T>> GetDeletedAsync(CancellationToken cancellationToken = default)
    {
        if (typeof(BaseEntity).IsAssignableFrom(typeof(T)))
        {
            // Bỏ global soft-delete filter, nếu không điều kiện IsDeleted = true sẽ không bao giờ khớp.
            return await _dbSet
                .IgnoreQueryFilters()
                .Where(e => ((BaseEntity)(object)e).IsDeleted)
                .ToListAsync(cancellationToken);
        }
        return await _dbSet.ToListAsync(cancellationToken);
    }

    // ========== COMMAND METHODS - AUTO SAVE ==========
    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public void Add(T entity)
    {
        _dbSet.Add(entity);
        _unitOfWork.SaveChangesAsync().GetAwaiter().GetResult();
    }

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public void AddRange(IEnumerable<T> entities)
    {
        _dbSet.AddRange(entities);
        _unitOfWork.SaveChangesAsync().GetAwaiter().GetResult();
    }

    public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public void Update(T entity)
    {
        _dbSet.Update(entity);
        _unitOfWork.SaveChangesAsync().GetAwaiter().GetResult();
    }

    public async Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        _dbSet.UpdateRange(entities);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public void UpdateRange(IEnumerable<T> entities)
    {
        _dbSet.UpdateRange(entities);
        _unitOfWork.SaveChangesAsync().GetAwaiter().GetResult();
    }

    public async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (entity is BaseEntity baseEntity)
        {
            baseEntity.IsDeleted = true;
            _dbSet.Update(entity);
        }
        else
        {
            _dbSet.Remove(entity);
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public void Delete(T entity)
    {
        if (entity is BaseEntity baseEntity)
        {
            baseEntity.IsDeleted = true;
            _dbSet.Update(entity);
        }
        else
        {
            _dbSet.Remove(entity);
        }
        _unitOfWork.SaveChangesAsync().GetAwaiter().GetResult();
    }

    public async Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities)
        {
            if (entity is BaseEntity baseEntity)
            {
                baseEntity.IsDeleted = true;
                _dbSet.Update(entity);
            }
            else
            {
                _dbSet.Remove(entity);
            }
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public void DeleteRange(IEnumerable<T> entities)
    {
        foreach (var entity in entities)
        {
            if (entity is BaseEntity baseEntity)
            {
                baseEntity.IsDeleted = true;
                _dbSet.Update(entity);
            }
            else
            {
                _dbSet.Remove(entity);
            }
        }
        _unitOfWork.SaveChangesAsync().GetAwaiter().GetResult();
    }

    public async Task RestoreAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (entity is BaseEntity baseEntity)
        {
            baseEntity.IsDeleted = false;
            _dbSet.Update(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public void Restore(T entity)
    {
        if (entity is BaseEntity baseEntity)
        {
            baseEntity.IsDeleted = false;
            _dbSet.Update(entity);
            _unitOfWork.SaveChangesAsync().GetAwaiter().GetResult();
        }
    }

    public async Task RestoreRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities)
        {
            if (entity is BaseEntity baseEntity)
            {
                baseEntity.IsDeleted = false;
                _dbSet.Update(entity);
            }
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public void RestoreRange(IEnumerable<T> entities)
    {
        foreach (var entity in entities)
        {
            if (entity is BaseEntity baseEntity)
            {
                baseEntity.IsDeleted = false;
                _dbSet.Update(entity);
            }
        }
        _unitOfWork.SaveChangesAsync().GetAwaiter().GetResult();
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _unitOfWork.SaveChangesAsync(cancellationToken);

    // ========== HARD DELETE (xoá vĩnh viễn, chỉ dùng cho màn "Đã xoá") ==========

    public void HardDelete(T entity)
    {
        _dbSet.Remove(entity);
        _unitOfWork.SaveChangesAsync().GetAwaiter().GetResult();
    }

    public void HardDeleteRange(IEnumerable<T> entities)
    {
        _dbSet.RemoveRange(entities);
        _unitOfWork.SaveChangesAsync().GetAwaiter().GetResult();
    }

    public async Task<int> HardDeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        _dbSet.RemoveRange(entities);
        return await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public IQueryable<T> GetQueryable()
        => _context.Set<T>().AsQueryable();

    public async Task<IQueryable<T>> GetQueryableAsync()
        => await Task.FromResult(_context.Set<T>().AsQueryable());
}