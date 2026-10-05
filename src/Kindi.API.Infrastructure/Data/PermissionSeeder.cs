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

        // Tầng 1 — node NHÓM (gốc cây, ParentCode = null). Seed trước để FK tự tham chiếu của màn hình thoả.
        var addedNodes = 0;
        foreach (var group in PermissionGroupCatalog.All)
        {
            if (byCode.TryGetValue(group.Code, out var currentGroup))
            {
                currentGroup.Name = group.Name;
                currentGroup.NameKey = PermissionNameKeys.Group(group.Code);
                currentGroup.NodeKind = PermissionNodeKind.Group;
                currentGroup.ParentCode = null;
                currentGroup.SortOrder = group.SortOrder;
                currentGroup.IsDeleted = false;
                continue;
            }

            var groupNode = new Permission
            {
                Code = group.Code,
                Name = group.Name,
                NameKey = PermissionNameKeys.Group(group.Code),
                NodeKind = PermissionNodeKind.Group,
                Module = PermissionModule.System,
                ParentCode = null,
                SortOrder = group.SortOrder
            };
            context.Permissions.Add(groupNode);
            byCode[group.Code] = groupNode;
            addedNodes++;
        }

        if (addedNodes > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        // Tầng 2 — node MÀN HÌNH (cha là nhóm). Seed trước hành động để ParentCode của hành động thoả FK.
        addedNodes = 0;
        foreach (var screen in PermissionScreenCatalog.All)
        {
            if (byCode.TryGetValue(screen.Code, out var currentScreen))
            {
                currentScreen.Name = screen.Name;
                currentScreen.NameKey = PermissionNameKeys.Screen(screen.Code);
                currentScreen.NodeKind = PermissionNodeKind.Screen;
                currentScreen.ParentCode = screen.Group;
                currentScreen.SortOrder = screen.SortOrder;
                currentScreen.IsDeleted = false;
                continue;
            }

            var screenNode = new Permission
            {
                Code = screen.Code,
                Name = screen.Name,
                NameKey = PermissionNameKeys.Screen(screen.Code),
                NodeKind = PermissionNodeKind.Screen,
                Module = PermissionModule.System,
                ParentCode = screen.Group,
                SortOrder = screen.SortOrder
            };
            context.Permissions.Add(screenNode);
            byCode[screen.Code] = screenNode;
            addedNodes++;
        }

        if (addedNodes > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        // Tầng 3 — node HÀNH ĐỘNG (cha là màn hình). Sửa phần mô tả theo enum — nguồn duy nhất là code.
        var added = 0;
        foreach (var definition in PermissionCatalog.All)
        {
            if (byCode.TryGetValue(definition.PermissionCode, out var current))
            {
                current.Name = definition.Name;
                current.NameKey = definition.NameKey;
                current.NodeKind = PermissionNodeKind.Action;
                current.Module = definition.Module;
                current.ParentCode = definition.Screen;
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
                NameKey = definition.NameKey,
                NodeKind = PermissionNodeKind.Action,
                Module = definition.Module,
                ParentCode = definition.Screen,
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

        // Kế thừa: cấp luôn các node cha (màn hình, nhóm) cho role đang có quyền con — nếu không, kiểm quyền
        // "hiệu lực = bản thân + mọi tổ tiên" sẽ chặn hết. Chỉ thêm dòng còn thiếu, giữ nguyên cấu hình đã sửa.
        var addedStructural = await EnsureStructuralGrantsAsync(context, byCode, cancellationToken);

        // Cùng luật cho quyền riêng theo tài khoản: cấp node cha cho quyền con tài khoản đang có.
        var addedUserStructural = await EnsureUserStructuralGrantsAsync(context, byCode, cancellationToken);

        // Chuyển quyền đã cấp (role + cấu hình riêng của tài khoản) từ mã gộp cũ sang các mã mới tách ra,
        // theo khai báo "Replaces" ở enum — bảo đảm không ai mất quyền đang có khi tách Xem/Sửa/Xoá.
        var copiedGrants = await CopyReplacedGrantsAsync(context, byCode, cancellationToken);

        logger?.LogInformation("Permission catalogue seeded: {Added} quyền mới, {Groups} nhóm mới, {Hidden} nhóm cũ đã ẩn, {Grants} gán role mới, {Structural} gán node cha theo role, {UserStructural} gán node cha theo tài khoản, {Copied} quyền chuyển tiếp đã sao chép",
            added, addedGroups, hiddenGroups, addedGrants, addedStructural, addedUserStructural, copiedGrants);
    }

    /// <summary>
    /// Cấp các node cha (màn hình, nhóm) cho mọi role đang có ít nhất một node con được cấp.
    /// Idempotent: chỉ thêm dòng còn thiếu và BẬT LẠI dòng cha đang tắt (quyền con chỉ có hiệu lực khi
    /// mọi tổ tiên đều được cấp — dòng cha bị tắt do lần lưu trước thiếu mã tổ tiên phải được sửa lại,
    /// nếu không quyền con đã cấp sẽ mãi ở trạng thái isGranted=true nhưng isEffective=false).
    /// Chỉ xử lý node cha của node đang được cấp nên KHÔNG bật lại quyền con nào bị tắt có chủ đích.
    /// </summary>
    private static async Task<int> EnsureStructuralGrantsAsync(
        ApplicationDbContext context,
        IReadOnlyDictionary<string, Permission> byCode,
        CancellationToken cancellationToken)
    {
        var roleRows = await context.RolePermissions.IgnoreQueryFilters().ToListAsync(cancellationToken);
        var idToCode = byCode.Values
            .GroupBy(x => x.Id)
            .ToDictionary(x => x.Key, x => x.First().Code);

        var existing = roleRows
            .GroupBy(x => (x.Role, x.PermissionId))
            .ToDictionary(x => x.Key, x => x.First());

        var changed = 0;
        foreach (var roleGroup in roleRows.Where(x => !x.IsDeleted && x.IsGranted).GroupBy(x => x.Role))
        {
            foreach (var ancestorCode in AncestorsNeeded(roleGroup.Select(x => x.PermissionId), idToCode))
            {
                if (!byCode.TryGetValue(ancestorCode, out var node)) continue;

                if (existing.TryGetValue((roleGroup.Key, node.Id), out var current))
                {
                    if (!current.IsGranted || current.IsDeleted)
                    {
                        current.IsGranted = true;
                        current.IsDeleted = false;
                        context.RolePermissions.Update(current);
                        changed++;
                    }
                    continue;
                }

                var row = new RolePermission { Role = roleGroup.Key, PermissionId = node.Id, IsGranted = true };
                context.RolePermissions.Add(row);
                existing[(roleGroup.Key, node.Id)] = row;
                changed++;
            }
        }

        if (changed > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        return changed;
    }

    /// <summary>
    /// Cấp các node cha cho quyền riêng của từng TÀI KHOẢN: quyền cấp riêng cho tài khoản cũng áp kế thừa
    /// khi tính hiệu lực, nên nếu tài khoản được cấp một hành động mà màn hình/nhóm chứa nó chưa được cấp
    /// (cả theo role lẫn theo tài khoản) thì hành động đó bị vô hiệu oan. Idempotent, chỉ bật/tạo node cha.
    /// </summary>
    private static async Task<int> EnsureUserStructuralGrantsAsync(
        ApplicationDbContext context,
        IReadOnlyDictionary<string, Permission> byCode,
        CancellationToken cancellationToken)
    {
        var userRows = await context.UserPermissions.IgnoreQueryFilters().ToListAsync(cancellationToken);
        var idToCode = byCode.Values
            .GroupBy(x => x.Id)
            .ToDictionary(x => x.Key, x => x.First().Code);

        var existing = userRows
            .GroupBy(x => (x.UserId, x.PermissionId))
            .ToDictionary(x => x.Key, x => x.First());

        var changed = 0;
        foreach (var userGroup in userRows.Where(x => !x.IsDeleted && x.IsGranted).GroupBy(x => x.UserId))
        {
            foreach (var ancestorCode in AncestorsNeeded(userGroup.Select(x => x.PermissionId), idToCode))
            {
                if (!byCode.TryGetValue(ancestorCode, out var node)) continue;

                if (existing.TryGetValue((userGroup.Key, node.Id), out var current))
                {
                    if (!current.IsGranted || current.IsDeleted)
                    {
                        current.IsGranted = true;
                        current.IsDeleted = false;
                        context.UserPermissions.Update(current);
                        changed++;
                    }
                    continue;
                }

                var row = new UserPermission { UserId = userGroup.Key, PermissionId = node.Id, IsGranted = true };
                context.UserPermissions.Add(row);
                existing[(userGroup.Key, node.Id)] = row;
                changed++;
            }
        }

        if (changed > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        return changed;
    }

    /// <summary>Mã các node cha (màn hình, nhóm) cần cấp cho tập quyền con đang được bật.</summary>
    private static HashSet<string> AncestorsNeeded(IEnumerable<Guid> grantedPermissionIds, IReadOnlyDictionary<Guid, string> idToCode)
    {
        var needed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var permissionId in grantedPermissionIds)
        {
            if (!idToCode.TryGetValue(permissionId, out var code)) continue;
            foreach (var ancestor in PermissionTreeCatalog.AncestorsOf(code)) needed.Add(ancestor);
        }

        return needed;
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
