namespace Kindi.API.UnitTests;

using System.Reflection;
using FluentAssertions;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Rules;
using Kindi.API.Shared.Constants;
using Kindi.API.WebApi.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

/// <summary>
/// Chốt quy tắc phân quyền ở mức build: mọi endpoint quản trị phải có mã quyền P###, không endpoint
/// nào được vô tình công khai, và danh mục quyền (enum PermissionCode) phải đầy đủ thông tin.
/// Test fail = build đỏ, buộc tính năng mới phải khai quyền trước khi merge.
/// </summary>
public class PermissionCoverageTests
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

    private static IEnumerable<AuthorizeAttribute> AuthorizeAttributes(Type controller, MethodInfo action)
        => action.GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Concat(controller.GetCustomAttributes<AuthorizeAttribute>(inherit: true));

    [Fact]
    public void Moi_endpoint_quan_tri_phai_co_ma_quyen()
    {
        var missing = new List<string>();

        foreach (var (controller, action) in GetEndpoints())
        {
            var attributes = AuthorizeAttributes(controller, action).ToList();
            var isAnonymous = action.IsDefined(typeof(AllowAnonymousAttribute), inherit: true)
                || controller.IsDefined(typeof(AllowAnonymousAttribute), inherit: true);
            if (isAnonymous)
                continue;

            // Chỉ endpoint dành cho quản trị mới bắt buộc có quyền; endpoint cho người dùng đã đăng nhập
            // sẽ được gắn quyền ở đợt phân quyền cho Khách hàng / Đối tác.
            var isAdminEndpoint = attributes.Any(a => a is not HasPermissionAttribute
                && (a.Roles ?? string.Empty).Split(',', StringSplitOptions.TrimEntries)
                    .Contains(RoleConstants.Admin));
            if (!isAdminEndpoint)
                continue;

            var hasPermission = action.IsDefined(typeof(HasPermissionAttribute), inherit: true)
                || controller.IsDefined(typeof(HasPermissionAttribute), inherit: true);
            if (!hasPermission)
            {
                missing.Add($"{controller.Name}.{action.Name}");
            }
        }

        missing.Should().BeEmpty(
            "endpoint quản trị phải khai báo [HasPermission(PermissionCode...)]: " + string.Join(", ", missing));
    }

    [Fact]
    public void Moi_endpoint_phai_cong_khai_co_chu_dich_hoac_duoc_bao_ve()
    {
        var unprotected = new List<string>();

        foreach (var (controller, action) in GetEndpoints())
        {
            var hasAuthorize = AuthorizeAttributes(controller, action).Any();
            var isAnonymous = action.IsDefined(typeof(AllowAnonymousAttribute), inherit: true)
                || controller.IsDefined(typeof(AllowAnonymousAttribute), inherit: true);

            if (!hasAuthorize && !isAnonymous)
            {
                unprotected.Add($"{controller.Name}.{action.Name}");
            }
        }

        unprotected.Should().BeEmpty(
            "endpoint phải có [Authorize] hoặc [AllowAnonymous] (không được vô tình mở công khai): "
            + string.Join(", ", unprotected));
    }

    [Fact]
    public void Danh_muc_quyen_day_du_va_dung_dinh_dang()
    {
        var definitions = PermissionCatalog.All;

        definitions.Should().NotBeEmpty();
        definitions.Select(x => x.PermissionCode).Should().OnlyHaveUniqueItems();
        definitions.Should().AllSatisfy(x =>
        {
            x.PermissionCode.Should().MatchRegex("^P[0-9]{3}$");
            x.Name.Should().NotBeNullOrWhiteSpace();
        });

        foreach (var code in Enum.GetValues<PermissionCode>())
        {
            definitions.Should().Contain(x => x.Code == code, $"mã {code} phải có trong danh mục");
        }
    }

    [Fact]
    public void Quyen_mac_dinh_cua_admin_phu_moi_nghiep_vu_va_khong_gom_quyen_cua_super_admin()
    {
        var adminDefaults = PermissionCatalog.DefaultFor(UserRole.Admin);

        var businessPermissions = PermissionCatalog.All
            .Where(x => x.Module != PermissionModule.SuperAdmin)
            .Select(x => x.Code);
        adminDefaults.Should().BeEquivalentTo(businessPermissions);

        PermissionCatalog.DefaultFor(UserRole.SuperAdmin).Should().BeEmpty(
            "SuperAdmin luôn toàn quyền, không lưu ở bảng RolePermissions");

        // User và Partner chỉ có quyền cơ bản của khu vực thành viên, không có quyền quản trị nào.
        var memberArea = new[]
        {
            PermissionCode.ViewMyReferralStats,
            PermissionCode.ViewMyGroupBuying,
            PermissionCode.ViewMyRequests,
            PermissionCode.ViewMyPosts,
            PermissionCode.ViewMyGroups
        };

        PermissionCatalog.DefaultFor(UserRole.User).Should().BeEquivalentTo(memberArea,
            "khách hàng chỉ dùng khu vực thành viên");
        PermissionCatalog.DefaultFor(UserRole.Partner).Should().BeEquivalentTo(memberArea,
            "đối tác là tài khoản khách hàng đã được duyệt hồ sơ");
        PermissionCatalog.DefaultFor(UserRole.User)
            .Concat(PermissionCatalog.DefaultFor(UserRole.Partner))
            .Should().NotIntersectWith(PermissionCatalog.All
                .Where(x => x.Module == PermissionModule.SuperAdmin)
                .Select(x => x.Code), "quyền của nhóm SuperAdmin chỉ thuộc SuperAdmin");
    }

    [Fact]
    public void Super_admin_khong_the_duoc_gan_qua_role()
    {
        RoleRules.IsAssignable(UserRole.SuperAdmin).Should().BeFalse();
        RoleRules.AssignableRoles.Should().NotContain(UserRole.SuperAdmin);
        RoleRules.ConfigurableRoles.Should().NotContain(UserRole.SuperAdmin);
    }

    [Fact]
    public void Chi_super_admin_moi_co_quyen_thuoc_nhom_super_admin()
    {
        var superAdminPermissions = PermissionCatalog.All
            .Where(x => x.Module == PermissionModule.SuperAdmin)
            .ToList();

        superAdminPermissions.Should().NotBeEmpty();
        PermissionCatalog.DefaultFor(UserRole.Admin).Should().NotIntersectWith(superAdminPermissions.Select(x => x.Code));
    }
}
