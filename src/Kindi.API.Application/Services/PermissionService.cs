namespace Kindi.API.Application.Services;

using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Domain.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

/// <summary>
/// Đọc/ghi quyền của từng role. Kết quả đọc được cache trong bộ nhớ để mỗi request không phải
/// truy vấn lại DB; cache bị xóa ngay khi ma trận quyền được sửa.
/// </summary>
public class PermissionService : IPermissionService
{
    private const string CacheKey = "kindi:permission-snapshot";
    private static readonly IReadOnlySet<string> Empty = new HashSet<string>();

    private readonly IQueryService _queryService;
    private readonly IRepository<RolePermission> _rolePermissionRepo;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        IQueryService queryService,
        IRepository<RolePermission> rolePermissionRepo,
        IMemoryCache cache,
        ILogger<PermissionService> logger)
    {
        _queryService = queryService;
        _rolePermissionRepo = rolePermissionRepo;
        _cache = cache;
        _logger = logger;
    }

    private sealed record Snapshot(IReadOnlyDictionary<UserRole, IReadOnlySet<string>> ByRole, long Version);

    public IReadOnlyList<PermissionDefinition> GetCatalog() => PermissionCatalog.All;

    public async Task<RolePermissions> GetRolePermissionsAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        var snapshot = await GetSnapshotAsync(cancellationToken);

        // SuperAdmin luôn toàn quyền (handler cũng bypass) — trả về cả danh mục để UI hiện đủ menu.
        if (role == UserRole.SuperAdmin)
            return new RolePermissions(PermissionCatalog.All.Select(x => x.PermissionCode).ToHashSet(), snapshot.Version);

        return new RolePermissions(snapshot.ByRole.TryGetValue(role, out var codes) ? codes : Empty, snapshot.Version);
    }

    public async Task SetRolePermissionsAsync(UserRole role, IEnumerable<string> permissionCodes, CancellationToken cancellationToken = default)
    {
        if (!RoleRules.IsAssignable(role))
            throw new InvalidOperationException("Không thể cấu hình quyền cho role này.");

        var wanted = permissionCodes
            .Select(PermissionCodeExtensions.FromCode)
            .Where(x => x.HasValue)
            .Select(x => x!.Value.ToCode())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var permissions = (await _queryService.GetAll<Permission>().ToListAsync(cancellationToken))
            .GroupBy(x => x.Id)
            .Select(x => x.First())
            .ToList();

        // Đọc cả dòng đã xoá mềm: bảng có ràng buộc duy nhất (Role, PermissionId) nên phải khôi phục
        // dòng cũ thay vì thêm mới, tránh lỗi trùng khoá.
        var current = await _rolePermissionRepo.GetQueryable()
            .IgnoreQueryFilters()
            .Where(x => x.Role == role)
            .ToListAsync(cancellationToken);
        var currentByPermission = current
            .GroupBy(x => x.PermissionId)
            .ToDictionary(x => x.Key, x => x.First());

        var added = new List<RolePermission>();
        var updated = new List<RolePermission>();

        foreach (var permission in permissions)
        {
            var isGranted = wanted.Contains(permission.Code);
            if (currentByPermission.TryGetValue(permission.Id, out var row))
            {
                if (row.IsDeleted)
                {
                    row.IsDeleted = false;
                    row.IsGranted = isGranted;
                    updated.Add(row);
                }
                else if (row.IsGranted != isGranted)
                {
                    row.IsGranted = isGranted;
                    updated.Add(row);
                }
                continue;
            }

            added.Add(new RolePermission
            {
                Role = role,
                PermissionId = permission.Id,
                IsGranted = isGranted
            });
        }

        if (updated.Count > 0)
            _rolePermissionRepo.UpdateRange(updated);

        if (added.Count > 0)
            await _rolePermissionRepo.AddRangeAsync(added, cancellationToken);

        if (updated.Count > 0)
            await _rolePermissionRepo.SaveChangesAsync(cancellationToken);
        InvalidateCache();
        _logger.LogInformation("Đã cập nhật quyền cho role {Role}: {Count} quyền bật", role, wanted.Count);
    }

    public void InvalidateCache() => _cache.Remove(CacheKey);

    private async Task<Snapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(CacheKey, out Snapshot? cached) && cached != null)
            return cached;

        var rows = await _queryService.GetAllNoTracking<RolePermission>()
            .Where(x => x.IsGranted)
            .Select(x => new { x.Role, Code = x.Permission!.Code, x.UpdatedAt, x.CreatedAt })
            .ToListAsync(cancellationToken);

        var byRole = rows
            .GroupBy(x => x.Role)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlySet<string>)g.Select(x => x.Code).ToHashSet(StringComparer.OrdinalIgnoreCase));

        var latest = rows
            .Select(x => x.UpdatedAt ?? x.CreatedAt)
            .DefaultIfEmpty(DateTime.UnixEpoch)
            .Max();
        var version = latest == DateTime.UnixEpoch ? 0L : latest.Ticks;

        var snapshot = new Snapshot(byRole, version);
        _cache.Set(CacheKey, snapshot, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

        return snapshot;
    }
}
