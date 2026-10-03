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
/// Đọc/ghi quyền của từng role và cấu hình quyền riêng của từng tài khoản. Kết quả đọc được cache
/// trong bộ nhớ để mỗi request không phải truy vấn lại DB; cache bị xóa ngay khi cấu hình quyền được sửa.
/// </summary>
public class PermissionService : IPermissionService
{
    private const string CacheKey = "kindi:permission-snapshot";
    private static readonly IReadOnlySet<string> Empty = new HashSet<string>();
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    private readonly IQueryService _queryService;
    private readonly IRepository<RolePermission> _rolePermissionRepo;
    private readonly IRepository<UserPermission> _userPermissionRepo;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        IQueryService queryService,
        IRepository<RolePermission> rolePermissionRepo,
        IRepository<UserPermission> userPermissionRepo,
        IMemoryCache cache,
        ILogger<PermissionService> logger)
    {
        _queryService = queryService;
        _rolePermissionRepo = rolePermissionRepo;
        _userPermissionRepo = userPermissionRepo;
        _cache = cache;
        _logger = logger;
    }

    private sealed record Snapshot(
        IReadOnlyDictionary<UserRole, IReadOnlySet<string>> ByRole,
        IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, bool>> UserOverrides,
        long Version);

    public IReadOnlyList<PermissionDefinition> GetCatalog() => PermissionCatalog.All;

    public async Task<RolePermissions> GetRolePermissionsAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        var snapshot = await GetSnapshotAsync(cancellationToken);

        // SuperAdmin luôn toàn quyền (handler cũng bypass) — trả về cả danh mục để UI hiện đủ menu.
        if (role == UserRole.SuperAdmin)
            return new RolePermissions(PermissionCatalog.All.Select(x => x.PermissionCode).ToHashSet(), snapshot.Version);

        return new RolePermissions(snapshot.ByRole.TryGetValue(role, out var codes) ? codes : Empty, snapshot.Version);
    }

    public async Task<UserPermissions> GetUserPermissionsAsync(Guid userId, UserRole role, CancellationToken cancellationToken = default)
    {
        var snapshot = await GetSnapshotAsync(cancellationToken);

        // SuperAdmin luôn toàn quyền, không cấu hình riêng.
        if (role == UserRole.SuperAdmin)
            return new UserPermissions(PermissionCatalog.All.Select(x => x.PermissionCode).ToHashSet(), Empty, Empty, snapshot.Version);

        var roleCodes = snapshot.ByRole.TryGetValue(role, out var codes) ? codes : Empty;

        if (!snapshot.UserOverrides.TryGetValue(userId, out var overrides) || overrides.Count == 0)
            return new UserPermissions(roleCodes, Empty, Empty, snapshot.Version);

        var granted = overrides.Where(x => x.Value).Select(x => x.Key).ToHashSet(Comparer);
        var denied = overrides.Where(x => !x.Value).Select(x => x.Key).ToHashSet(Comparer);
        var effective = roleCodes.Except(denied, Comparer).Union(granted, Comparer).ToHashSet(Comparer);

        return new UserPermissions(effective, granted, denied, snapshot.Version);
    }

    public async Task<UserPermissionDetail?> GetUserPermissionDetailAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = (await _queryService.GetAllNoTracking<User>()
                .Where(x => x.Id == userId && !x.IsDeleted)
                .ToListAsync(cancellationToken))
            .FirstOrDefault();

        if (user == null)
            return null;

        var rolePermissions = await GetRolePermissionsAsync(user.Role, cancellationToken);
        var userPermissions = await GetUserPermissionsAsync(user.Id, user.Role, cancellationToken);

        return new UserPermissionDetail(
            user.Id,
            user.Username,
            user.FullName,
            user.Role,
            rolePermissions.Codes,
            userPermissions.GrantedCodes,
            userPermissions.DeniedCodes,
            userPermissions.Codes);
    }

    public async Task<IReadOnlyList<UserPermissionCandidate>> SearchUsersAsync(string? search, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _queryService.GetAllNoTracking<User>()
            .Where(x => !x.IsDeleted && x.Role != UserRole.SuperAdmin);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x => x.Username.Contains(keyword) || x.FullName.Contains(keyword));
        }

        var rows = await query
            .OrderBy(x => x.Username)
            .Skip((Math.Max(pageNumber, 1) - 1) * Math.Clamp(pageSize, 1, 100))
            .Take(Math.Clamp(pageSize, 1, 100))
            .Select(x => new { x.Id, x.Username, x.FullName, x.Role })
            .ToListAsync(cancellationToken);

        return rows.Select(x => new UserPermissionCandidate(x.Id, x.Username, x.FullName, x.Role)).ToList();
    }

    public async Task SetRolePermissionsAsync(UserRole role, IEnumerable<string> permissionCodes, CancellationToken cancellationToken = default)
    {
        if (!RoleRules.IsAssignable(role))
            throw new InvalidOperationException("Không thể cấu hình quyền cho role này.");

        var wanted = Normalize(permissionCodes);

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

    public async Task SetUserPermissionsAsync(IReadOnlyCollection<Guid> userIds, IEnumerable<string> permissionCodes, CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
            throw new InvalidOperationException("Chưa chọn tài khoản nào để cấu hình quyền.");

        var wanted = Normalize(permissionCodes);

        var permissions = (await _queryService.GetAll<Permission>().ToListAsync(cancellationToken))
            .GroupBy(x => x.Id)
            .Select(x => x.First())
            .ToList();

        var users = await _queryService.GetAllNoTracking<User>()
            .Where(x => ids.Contains(x.Id) && !x.IsDeleted)
            .ToListAsync(cancellationToken);
        if (users.Count != ids.Count)
            throw new InvalidOperationException("Có tài khoản không tồn tại.");

        var snapshot = await GetSnapshotAsync(cancellationToken);

        // Đọc cả dòng đã xoá mềm: bảng có ràng buộc duy nhất (UserId, PermissionId) nên phải khôi phục dòng cũ.
        var current = await _userPermissionRepo.GetQueryable()
            .IgnoreQueryFilters()
            .Where(x => ids.Contains(x.UserId))
            .ToListAsync(cancellationToken);
        var currentByKey = current
            .GroupBy(x => (x.UserId, x.PermissionId))
            .ToDictionary(x => x.Key, x => x.First());

        var added = new List<UserPermission>();
        var updated = new List<UserPermission>();

        foreach (var user in users.Where(x => x.Role != UserRole.SuperAdmin))
        {
            var roleCodes = snapshot.ByRole.TryGetValue(user.Role, out var codes) ? codes : Empty;

            foreach (var permission in permissions)
            {
                var roleHas = roleCodes.Contains(permission.Code);
                var desired = wanted.Contains(permission.Code);
                var hasRow = currentByKey.TryGetValue((user.Id, permission.Id), out var row);

                // Giống quyền của role thì tài khoản không cần dòng riêng.
                if (desired == roleHas)
                {
                    if (hasRow && row != null && !row.IsDeleted)
                    {
                        row.IsDeleted = true;
                        updated.Add(row);
                    }
                    continue;
                }

                if (hasRow && row != null)
                {
                    if (row.IsDeleted || row.IsGranted != desired)
                    {
                        row.IsDeleted = false;
                        row.IsGranted = desired;
                        updated.Add(row);
                    }
                    continue;
                }

                added.Add(new UserPermission
                {
                    UserId = user.Id,
                    PermissionId = permission.Id,
                    IsGranted = desired
                });
            }
        }

        if (updated.Count > 0)
            _userPermissionRepo.UpdateRange(updated);

        if (added.Count > 0)
            await _userPermissionRepo.AddRangeAsync(added, cancellationToken);

        if (updated.Count > 0)
            await _userPermissionRepo.SaveChangesAsync(cancellationToken);

        InvalidateCache();
        _logger.LogInformation("Đã cấu hình quyền riêng cho {Users} tài khoản: {Count} quyền bật", users.Count, wanted.Count);
    }

    public void InvalidateCache() => _cache.Remove(CacheKey);

    /// <summary>Chuẩn hoá danh sách mã quyền gửi lên về dạng chuẩn P### (bỏ mã không hợp lệ, bỏ trùng).</summary>
    private static HashSet<string> Normalize(IEnumerable<string> permissionCodes)
        => permissionCodes
            .Select(PermissionCodeExtensions.FromCode)
            .Where(x => x.HasValue)
            .Select(x => x!.Value.ToCode())
            .ToHashSet(Comparer);

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

        var overrideRows = await _queryService.GetAllNoTracking<UserPermission>()
            .Select(x => new { x.UserId, Code = x.Permission!.Code, x.IsGranted, x.UpdatedAt, x.CreatedAt })
            .ToListAsync(cancellationToken);

        var userOverrides = overrideRows
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyDictionary<string, bool>)g
                    .GroupBy(x => x.Code)
                    .ToDictionary(x => x.Key, x => x.Last().IsGranted, StringComparer.OrdinalIgnoreCase));

        var latest = rows.Select(x => x.UpdatedAt ?? x.CreatedAt)
            .Concat(overrideRows.Select(x => x.UpdatedAt ?? x.CreatedAt))
            .DefaultIfEmpty(DateTime.UnixEpoch)
            .Max();
        var version = latest == DateTime.UnixEpoch ? 0L : latest.Ticks;

        var snapshot = new Snapshot(byRole, userOverrides, version);
        _cache.Set(CacheKey, snapshot, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

        return snapshot;
    }
}
