namespace Kindi.API.UnitTests;

using System.Reflection;
using FluentAssertions;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Infrastructure.Data;
using Kindi.API.Shared.Common.Interfaces;
using Kindi.API.WebApi.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Chốt cấu trúc tách quyền Xem/Sửa/Xoá: mã mới có màn hình để render Nhóm → Màn hình → hành động,
/// khai báo ánh xạ từ mã gộp cũ, endpoint dùng mã mới vẫn chấp nhận mã cũ trong giai đoạn chuyển tiếp,
/// và seeder sao chép quyền đã cấp sang mã mới mà không làm ai mất quyền.
/// </summary>
public class PermissionSplitTests
{
    private static readonly Assembly WebApiAssembly = typeof(HasPermissionAttribute).Assembly;

    private static IEnumerable<(Type Controller, MethodInfo Action)> GetEndpoints()
    {
        var controllers = WebApiAssembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(ControllerBase).IsAssignableFrom(t));

        foreach (var controller in controllers)
        {
            foreach (var action in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (action.DeclaringType == typeof(ControllerBase) || action.GetCustomAttribute<HttpMethodAttribute>() == null)
                    continue;

                if (action.IsDefined(typeof(NonActionAttribute), inherit: true))
                    continue;

                yield return (controller, action);
            }
        }
    }

    private static IReadOnlySet<string> EndpointPermissionCodes(Type controller, MethodInfo action)
    {
        var attribute = action.GetCustomAttribute<HasPermissionAttribute>(inherit: true)
            ?? controller.GetCustomAttribute<HasPermissionAttribute>(inherit: true);
        return attribute is null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : attribute.PermissionCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Moi_quyen_deu_co_man_hinh_de_render_theo_nhom_man_hinh_hanh_dong()
    {
        var definitions = PermissionCatalog.All;

        definitions.Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.Screen),
            "mỗi quyền phải khai màn hình để gom nhóm ở ma trận phân quyền");
        definitions.Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.ScreenName),
            "màn hình phải có tên hiển thị");

        var catalogCodes = PermissionScreenCatalog.All.Select(x => x.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        definitions.Select(x => x.Screen).Distinct()
            .Should().OnlyContain(screen => catalogCodes.Contains(screen),
                "mọi màn hình dùng ở enum phải có trong PermissionScreenCatalog");

        // Cấu trúc Nhóm → Màn hình → hành động: mỗi nhóm chức năng phải có ít nhất một quyền.
        definitions.GroupBy(x => x.Module).Should().OnlyContain(g => g.Any());
    }

    [Fact]
    public void Cac_man_hinh_tach_xem_sua_xoa_dung_kind()
    {
        PermissionCatalog.Get(PermissionCode.ViewRestoreCollaborator).Kind.Should().Be(PermissionKind.View);
        PermissionCatalog.Get(PermissionCode.RestorePartner).Kind.Should().Be(PermissionKind.Delete);
        PermissionCatalog.Get(PermissionCode.DeletePartnerProduct).Kind.Should().Be(PermissionKind.Delete);
        PermissionCatalog.Get(PermissionCode.RestoreOfferRequest).Kind.Should().Be(PermissionKind.Delete);
        PermissionCatalog.Get(PermissionCode.DeleteGroupBuyingRequest).Kind.Should().Be(PermissionKind.Delete);
        PermissionCatalog.Get(PermissionCode.DeleteGroup).Kind.Should().Be(PermissionKind.Delete);
        PermissionCatalog.Get(PermissionCode.DeleteCommissionConfig).Kind.Should().Be(PermissionKind.Delete);

        PermissionCatalog.Get(PermissionCode.ViewMembershipTiers).Kind.Should().Be(PermissionKind.View);
        PermissionCatalog.Get(PermissionCode.UpdateMembershipTiers).Kind.Should().Be(PermissionKind.Update);
        PermissionCatalog.Get(PermissionCode.DeleteMembershipTiers).Kind.Should().Be(PermissionKind.Delete);

        PermissionCatalog.Get(PermissionCode.ViewBankAccounts).Kind.Should().Be(PermissionKind.View);

        PermissionCatalog.Get(PermissionCode.ViewRevenueConfig).Kind.Should().Be(PermissionKind.View);
        PermissionCatalog.Get(PermissionCode.UpdateRevenueConfig).Kind.Should().Be(PermissionKind.Update);
        PermissionCatalog.Get(PermissionCode.DeleteRevenueConfig).Kind.Should().Be(PermissionKind.Delete);

        // Cùng màn hình phải gom được các hành động lại với nhau.
        PermissionCatalog.Get(PermissionCode.ViewMembershipTiers).Screen
            .Should().Be(PermissionCatalog.Get(PermissionCode.DeleteMembershipTiers).Screen);
    }

    [Fact]
    public void Ma_tach_moi_deu_khai_bao_anh_xa_tu_ma_gop_cu()
    {
        var actual = PermissionCatalog.Replacements.Select(x => $"{x.OldCode}->{x.NewCode}").ToHashSet();

        var expected = new[]
        {
            "P027->P115",
            "P045->P116",
            "P046->P118",
            "P065->P119",
            "P067->P120",
            "P071->P121",
            "P106->P124",
            "P109->P125",
            "P107->P125",
            "P109->P126",
            "P109->P127",
            "P110->P128",
            "P114->P129",
            "P114->P130",
            "P114->P131"
        };

        actual.Should().Contain(expected, "mọi mã tách mới phải khai báo mã gộp cũ mà nó thay thế");
    }

    [Fact]
    public void Ma_tach_moi_khong_mo_coi()
    {
        // Mọi mã mới (có Replaces) phải thực sự được gắn cho ít nhất một endpoint để tránh mã mồ côi.
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (controller, action) in GetEndpoints())
            used.UnionWith(EndpointPermissionCodes(controller, action));

        var orphans = PermissionCatalog.All
            .Where(x => x.Replaces.Count > 0 && !used.Contains(x.PermissionCode))
            .Select(x => x.PermissionCode)
            .ToList();

        orphans.Should().BeEmpty("mã tách mới không được mồ côi (phải gác một endpoint thật)");
    }

    [Fact]
    public void Endpoint_dung_ma_moi_van_chap_nhan_ca_ma_cu_chuyen_tiep()
    {
        // Giai đoạn chuyển tiếp: token đang đăng nhập còn mang mã cũ nên mọi endpoint dùng mã mới
        // phải chấp nhận thêm mã cũ mà endpoint đó vốn dùng — nếu không, người dùng bị đá ra tới khi đăng nhập lại.
        var transitionFallback = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["P115"] = "P027",
            ["P116"] = "P045",
            ["P118"] = "P046",
            ["P119"] = "P065",
            ["P120"] = "P067",
            ["P121"] = "P071",
            ["P124"] = "P106",
            ["P125"] = "P107", // GET /membership-tiers vốn gác bằng P107 (xem chi trả), dù quyền cấp chuyển từ P109.
            ["P126"] = "P109",
            ["P127"] = "P109",
            ["P128"] = "P110",
            ["P129"] = "P114",
            ["P130"] = "P114",
            ["P131"] = "P114"
        };

        var violations = new List<string>();
        foreach (var (controller, action) in GetEndpoints())
        {
            var codes = EndpointPermissionCodes(controller, action);
            foreach (var code in codes)
            {
                if (!transitionFallback.TryGetValue(code, out var oldCode)) continue;
                if (!codes.Contains(oldCode))
                    violations.Add($"{controller.Name}.{action.Name}: mã mới {code} thiếu mã cũ {oldCode}");
            }
        }

        violations.Should().BeEmpty("endpoint dùng mã mới phải chấp nhận cả mã cũ trong giai đoạn chuyển tiếp");
    }

    [Fact]
    public void Ma_xem_moi_khong_duoc_cap_mac_dinh_tran_lan_cho_thanh_vien()
    {
        var memberDefaults = PermissionCatalog.DefaultFor(UserRole.User)
            .Concat(PermissionCatalog.DefaultFor(UserRole.Partner))
            .ToList();

        memberDefaults.Should().NotContain(PermissionCode.ViewMembershipTiers);
        memberDefaults.Should().NotContain(PermissionCode.ViewBankAccounts);
        memberDefaults.Should().NotContain(PermissionCode.ViewRevenueConfig);
        memberDefaults.Should().NotContain(PermissionCode.DeleteMembershipTiers);
    }

    [Fact]
    public async Task Seeder_chuyen_quyen_da_cap_sang_ma_moi_va_khong_lam_mat_quyen()
    {
        await using var context = CreateContext();

        // Lần seed đầu: tạo danh mục + quyền mặc định của Admin (toàn bộ nhóm nghiệp vụ).
        await PermissionSeeder.SeedAsync(context);
        var byCode = await context.Permissions.IgnoreQueryFilters()
            .ToDictionaryAsync(x => x.Code, x => x.Id, StringComparer.OrdinalIgnoreCase);

        // Giả lập cấu hình cũ: Admin được cấp riêng các mã nhóm SuperAdmin (P106/P109/P110/P114),
        // và một tài khoản được cấp riêng P109 nhưng đang TẮT (deny) — cả hai phải được chuyển tiếp.
        var userId = Guid.NewGuid();
        await AddRoleGrantAsync(context, UserRole.Admin, byCode["P106"]);
        await AddRoleGrantAsync(context, UserRole.Admin, byCode["P109"]);
        await AddRoleGrantAsync(context, UserRole.Admin, byCode["P110"]);
        await AddRoleGrantAsync(context, UserRole.Admin, byCode["P114"]);
        context.UserPermissions.Add(new UserPermission { UserId = userId, PermissionId = byCode["P109"], IsGranted = false });
        await context.SaveChangesAsync();

        // Lần seed kế tiếp: sao chép quyền cũ sang mã mới.
        await PermissionSeeder.SeedAsync(context);

        var adminGranted = await context.RolePermissions.IgnoreQueryFilters()
            .Where(x => x.Role == UserRole.Admin && !x.IsDeleted && x.IsGranted)
            .Select(x => x.PermissionId)
            .ToListAsync();

        // Các mã nhóm SuperAdmin không nằm trong quyền mặc định nên chỉ có thể đến từ bước sao chép.
        foreach (var newCode in new[] { "P124", "P125", "P126", "P127", "P128", "P129", "P130", "P131" })
            adminGranted.Should().Contain(byCode[newCode], $"Admin đang giữ mã cũ phải nhận được {newCode}");

        var userRows = await context.UserPermissions.IgnoreQueryFilters()
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .ToListAsync();

        foreach (var newCode in new[] { "P125", "P126", "P127" })
        {
            userRows.Should().ContainSingle(x => x.PermissionId == byCode[newCode] && !x.IsGranted,
                $"quyền tắt riêng của tài khoản phải được giữ nguyên khi chuyển sang {newCode}");
        }

        // Idempotent: chạy lại seeder không sinh thêm dòng.
        var roleCount = await context.RolePermissions.IgnoreQueryFilters().CountAsync();
        var userCount = await context.UserPermissions.IgnoreQueryFilters().CountAsync();
        await PermissionSeeder.SeedAsync(context);
        (await context.RolePermissions.IgnoreQueryFilters().CountAsync()).Should().Be(roleCount);
        (await context.UserPermissions.IgnoreQueryFilters().CountAsync()).Should().Be(userCount);
    }

    private static async Task AddRoleGrantAsync(ApplicationDbContext context, UserRole role, Guid permissionId)
    {
        context.RolePermissions.Add(new RolePermission { Role = role, PermissionId = permissionId, IsGranted = true });
        await context.SaveChangesAsync();
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"kindi-permission-split-{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options, new StubCurrentUserService());
    }

    private sealed class StubCurrentUserService : ICurrentUserService
    {
        public string? UserId => null;
        public string? UserName => "system";
        public bool IsAuthenticated => false;
        public bool IsInRole(string role) => false;
        public string? IpAddress => null;
        public string? UserAgent => null;
    }
}
