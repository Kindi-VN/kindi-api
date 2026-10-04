namespace Kindi.API.Infrastructure.Data;

using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Seed danh mục quyền từ enum <see cref="PermissionCode"/> và gán quyền mặc định cho từng role.
/// Idempotent: chạy lại không nhân bản, không ghi đè cấu hình quyền mà SuperAdmin đã sửa
/// (chỉ thêm quyền mới chưa có trong bảng).
/// </summary>
public static class PermissionSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        // Nhóm quyền: chỉ thêm nhóm còn thiếu để giữ nguyên tên/thứ tự đã sửa trong DB.
        var existingGroupCodes = await context.PermissionGroups
            .IgnoreQueryFilters()
            .Select(x => x.Code)
            .ToListAsync(cancellationToken);
        var groupCodes = existingGroupCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var addedGroups = 0;
        foreach (var group in PermissionGroupCatalog.All)
        {
            if (groupCodes.Contains(group.Code)) continue;

            context.PermissionGroups.Add(new PermissionGroup
            {
                Code = group.Code,
                Name = group.Name,
                NameEn = group.NameEn,
                SortOrder = group.SortOrder
            });
            groupCodes.Add(group.Code);
            addedGroups++;
        }

        if (addedGroups > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        var existing = await context.Permissions.IgnoreQueryFilters().ToListAsync(cancellationToken);
        var byCode = existing.ToDictionary(x => x.Code, x => x, StringComparer.OrdinalIgnoreCase);

        var added = 0;
        foreach (var definition in PermissionCatalog.All)
        {
            if (byCode.TryGetValue(definition.PermissionCode, out var current))
            {
                // Cập nhật lại phần mô tả (tên/nhóm/route/endpoint) theo enum — nguồn duy nhất là code.
                current.Name = definition.Name;
                current.Module = definition.Module;
                current.ParentCode = definition.ParentCode;
                current.Kind = definition.Kind;
                current.Route = definition.Route;
                current.Endpoints = definition.Endpoints;
                current.SortOrder = (int)definition.Code;
                current.IsDeleted = false;
                continue;
            }

            var entity = new Permission
            {
                Code = definition.PermissionCode,
                Name = definition.Name,
                Module = definition.Module,
                ParentCode = definition.ParentCode,
                Kind = definition.Kind,
                Route = definition.Route,
                Endpoints = definition.Endpoints,
                SortOrder = (int)definition.Code
            };

            context.Permissions.Add(entity);
            byCode[definition.PermissionCode] = entity;
            added++;
        }

        await context.SaveChangesAsync(cancellationToken);

        // Nhóm cũ không còn trong danh mục: ẩn đi để màn phân quyền không còn nhóm lạc, nhưng chỉ ẩn khi
        // không còn quyền nào trỏ tới (nếu còn quyền tham chiếu thì giữ nguyên nhóm đó).
        var catalogCodes = PermissionGroupCatalog.All.Select(x => x.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var usedGroupCodes = (await context.Permissions
                .IgnoreQueryFilters()
                .Where(x => x.ParentCode != null)
                .Select(x => x.ParentCode!)
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var hiddenGroups = 0;
        foreach (var group in await context.PermissionGroups.IgnoreQueryFilters()
                     .Where(x => !x.IsDeleted)
                     .ToListAsync(cancellationToken))
        {
            if (catalogCodes.Contains(group.Code) || usedGroupCodes.Contains(group.Code)) continue;

            group.IsDeleted = true;
            hiddenGroups++;
        }

        if (hiddenGroups > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        // Quyền mặc định cho role: chỉ thêm dòng còn thiếu, giữ nguyên cấu hình đã sửa.
        var grantedPairs = await context.RolePermissions
            .IgnoreQueryFilters()
            .Select(x => new { x.Role, x.PermissionId })
            .ToListAsync(cancellationToken);
        var granted = grantedPairs.Select(x => (x.Role, x.PermissionId)).ToHashSet();

        var addedGrants = 0;
        foreach (var role in new[] { UserRole.User, UserRole.Partner, UserRole.Admin })
        {
            foreach (var permission in PermissionCatalog.DefaultFor(role))
            {
                var permissionId = byCode[permission.ToCode()].Id;
                if (granted.Contains((role, permissionId))) continue;

                context.RolePermissions.Add(new RolePermission
                {
                    Role = role,
                    PermissionId = permissionId,
                    IsGranted = true
                });
                addedGrants++;
            }
        }

        if (addedGrants > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        // Chuyển quyền đã cấp (role + cấu hình riêng của tài khoản) từ mã gộp cũ sang các mã mới tách ra,
        // theo khai báo "Replaces" ở enum — bảo đảm không ai mất quyền đang có khi tách Xem/Sửa/Xoá.
        var copiedGrants = await CopyReplacedGrantsAsync(context, byCode, cancellationToken);

        logger?.LogInformation("Permission catalogue seeded: {Added} quyền mới, {Groups} nhóm mới, {Hidden} nhóm cũ đã ẩn, {Grants} gán role mới, {Copied} quyền chuyển tiếp đã sao chép",
            added, addedGroups, hiddenGroups, addedGrants, copiedGrants);
    }

    /// <summary>
    /// Sao chép quyền đã cấp từ mã cũ sang mã mới theo cặp ở <see cref="PermissionCatalog.Replacements"/>.
    /// Idempotent: chỉ thêm dòng còn thiếu hoặc khôi phục dòng bị xoá mềm, không ghi đè cấu hình đã có;
    /// giữ nguyên cả dòng tắt quyền riêng (<c>IsGranted = false</c>) để không vô tình bật lại quyền đã tắt.
    /// </summary>
    private static async Task<int> CopyReplacedGrantsAsync(
        ApplicationDbContext context,
        IReadOnlyDictionary<string, Permission> byCode,
        CancellationToken cancellationToken)
    {
        var pairs = PermissionCatalog.Replacements
            .Where(x => byCode.ContainsKey(x.OldCode) && byCode.ContainsKey(x.NewCode))
            .ToList();
        if (pairs.Count == 0) return 0;

        // Đọc cả dòng xoá mềm: bảng có ràng buộc duy nhất (role/user, quyền) nên phải tái dùng dòng cũ,
        // tránh lỗi trùng khoá khi thêm mới.
        var roleRows = await context.RolePermissions.IgnoreQueryFilters().ToListAsync(cancellationToken);
        var roleByKey = roleRows
            .GroupBy(x => (x.Role, x.PermissionId))
            .ToDictionary(x => x.Key, x => x.First());

        var userRows = await context.UserPermissions.IgnoreQueryFilters().ToListAsync(cancellationToken);
        var userByKey = userRows
            .GroupBy(x => (x.UserId, x.PermissionId))
            .ToDictionary(x => x.Key, x => x.First());

        var changed = 0;

        foreach (var pair in pairs)
        {
            var oldId = byCode[pair.OldCode].Id;
            var newId = byCode[pair.NewCode].Id;

            // Chỉ lấy dòng đang hiệu lực (chưa xoá mềm) làm nguồn sao chép.
            foreach (var source in roleRows.Where(x => x.PermissionId == oldId && !x.IsDeleted))
            {
                var key = (source.Role, newId);
                if (roleByKey.TryGetValue(key, out var existing))
                {
                    if (existing.IsDeleted)
                    {
                        existing.IsDeleted = false;
                        existing.IsGranted = source.IsGranted;
                        changed++;
                    }
                    continue;
                }

                var row = new RolePermission { Role = source.Role, PermissionId = newId, IsGranted = source.IsGranted };
                context.RolePermissions.Add(row);
                roleByKey[key] = row;
                changed++;
            }

            foreach (var source in userRows.Where(x => x.PermissionId == oldId && !x.IsDeleted))
            {
                var key = (source.UserId, newId);
                if (userByKey.TryGetValue(key, out var existing))
                {
                    if (existing.IsDeleted)
                    {
                        existing.IsDeleted = false;
                        existing.IsGranted = source.IsGranted;
                        changed++;
                    }
                    continue;
                }

                var row = new UserPermission { UserId = source.UserId, PermissionId = newId, IsGranted = source.IsGranted };
                context.UserPermissions.Add(row);
                userByKey[key] = row;
                changed++;
            }
        }

        if (changed > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        return changed;
    }
}
