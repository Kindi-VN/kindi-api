namespace Kindi.API.UnitTests;

using FluentAssertions;
using Kindi.API.Application.Services;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Infrastructure.Data;
using Kindi.API.Infrastructure.Repositories;
using Kindi.API.Shared.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>
/// Quyền hiệu lực = bản thân được cấp VÀ MỌI tổ tiên (màn hình, nhóm) đều được cấp.
/// Bộ test này chốt: không được lưu một hành động mà thiếu mã tổ tiên của nó — nếu không, quyền vừa bật
/// sẽ có <c>isGranted=true</c> nhưng <c>isEffective=false</c> và người dùng bị chặn oan (đã gặp với
/// "xem hoa hồng": tick ở màn phân quyền báo đã nhận nhưng màn hoa hồng vẫn khoá).
/// </summary>
public class PermissionAncestorGrantTests
{
    [Fact]
    public void Quyen_hanh_dong_thieu_to_tien_thi_bi_ke_thua_vo_hieu()
    {
        // P013 = "Xem hoa hồng của tôi" → tổ tiên: màn hình MY_COMMISSION → nhóm MEMBER.
        PermissionTreeCatalog.AncestorsOf("P013")
            .Should().Equal("MY_COMMISSION", "MEMBER");

        // Chỉ cấp mã hành động (thiếu màn hình + nhóm) ⇒ không hiệu lực.
        PermissionTreeCatalog.ApplyInheritance(new[] { "P013" })
            .Should().BeEmpty("thiếu tổ tiên MY_COMMISSION/MEMBER nên hành động bị vô hiệu");

        // Cấp kèm đủ chuỗi tổ tiên ⇒ hành động có hiệu lực.
        PermissionTreeCatalog.ApplyInheritance(new[] { "P013", "MY_COMMISSION", "MEMBER" })
            .Should().Contain("P013");

        // P105 = "Xem cấu hình hoa hồng" (màn quản trị) → tổ tiên: màn hình COMMISSION_CONFIG → nhóm ADMIN.
        PermissionTreeCatalog.AncestorsOf("P105")
            .Should().Equal("COMMISSION_CONFIG", "ADMIN");
        PermissionTreeCatalog.ApplyInheritance(new[] { "P105", "COMMISSION_CONFIG", "ADMIN" })
            .Should().Contain("P105");
    }

    [Fact]
    public async Task Luu_quyen_theo_vai_tro_tu_cap_chuoi_to_tien()
    {
        await using var context = CreateContext();
        await PermissionSeeder.SeedAsync(context);
        var service = CreateService(context);

        // Giao diện chỉ gửi mã hành động đang tick — máy chủ phải tự cấp kèm màn hình + nhóm.
        await service.SetRolePermissionsAsync(UserRole.Admin, new[] { "P105" });

        var granted = await service.GetRolePermissionsAsync(UserRole.Admin);
        granted.Codes.Should().Contain(new[] { "P105", "COMMISSION_CONFIG", "ADMIN" });

        // Và cây trả về cho vai trò đó phải cho thấy P105 hiệu lực thật.
        var tree = await service.GetTreeAsync((int)UserRole.Admin);
        var actions = Flatten(tree).Where(x => x.ParentCode == "COMMISSION_CONFIG").ToList();
        actions.Should().Contain(x => x.Code == "P105" && x.IsGranted && x.IsEffective);
    }

    [Fact]
    public async Task Luu_quyen_theo_tai_khoan_tu_cap_chuoi_to_tien()
    {
        await using var context = CreateContext();
        await PermissionSeeder.SeedAsync(context);
        var service = CreateService(context);

        var user = new User { Username = "hoa-hong-test", FullName = "Hoa hồng test", Role = UserRole.User };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Tick "xem hoa hồng của tôi" cho tài khoản: chỉ mã hành động được gửi lên.
        await service.SetUserPermissionsAsync(new[] { user.Id }, new[] { "P013" });

        var detail = await service.GetUserPermissionDetailAsync(user.Id);
        detail.Should().NotBeNull();
        detail!.GrantedCodes.Should().Contain("P013");
        detail.EffectiveCodes.Should().Contain(new[] { "P013", "MY_COMMISSION", "MEMBER" },
            "quyền xem hoa hồng phải hiệu lực, không bị kế thừa vô hiệu vì thiếu màn hình/nhóm");
    }

    [Fact]
    public async Task Seeder_bat_lai_node_cha_bi_tat_cho_quyen_da_cap()
    {
        await using var context = CreateContext();
        await PermissionSeeder.SeedAsync(context);
        var byCode = await context.Permissions.IgnoreQueryFilters()
            .ToDictionaryAsync(x => x.Code, x => x.Id, StringComparer.OrdinalIgnoreCase);

        // Dữ liệu hỏng kiểu cũ: cấp hành động nhưng thiếu mã màn hình/nhóm (đúng trạng thái đang có trên
        // production với tài khoản test "xem hoa hồng": P013 granted nhưng không hiệu lực).
        var userId = Guid.NewGuid();
        context.RolePermissions.Add(new RolePermission { Role = UserRole.Admin, PermissionId = byCode["P105"], IsGranted = true });
        context.UserPermissions.Add(new UserPermission { UserId = userId, PermissionId = byCode["P013"], IsGranted = true });
        await context.SaveChangesAsync();

        // Seeder phải bật lại/tạo node cha cho quyền con đã cấp (cả theo vai trò lẫn theo tài khoản).
        await PermissionSeeder.SeedAsync(context);

        var adminGranted = await context.RolePermissions.IgnoreQueryFilters()
            .Where(x => x.Role == UserRole.Admin && !x.IsDeleted && x.IsGranted)
            .Select(x => x.PermissionId)
            .ToListAsync();
        adminGranted.Should().Contain(new[] { byCode["COMMISSION_CONFIG"], byCode["ADMIN"] });

        var userGranted = await context.UserPermissions.IgnoreQueryFilters()
            .Where(x => x.UserId == userId && !x.IsDeleted && x.IsGranted)
            .Select(x => x.PermissionId)
            .ToListAsync();
        userGranted.Should().Contain(new[] { byCode["MY_COMMISSION"], byCode["MEMBER"] });

        // Idempotent: chạy lại không sinh thêm dòng.
        var roleCount = await context.RolePermissions.IgnoreQueryFilters().CountAsync();
        var userCount = await context.UserPermissions.IgnoreQueryFilters().CountAsync();
        await PermissionSeeder.SeedAsync(context);
        (await context.RolePermissions.IgnoreQueryFilters().CountAsync()).Should().Be(roleCount);
        (await context.UserPermissions.IgnoreQueryFilters().CountAsync()).Should().Be(userCount);

        // Không tự bật lại quyền con bị tắt có chủ đích.
        context.UserPermissions.Add(new UserPermission { UserId = userId, PermissionId = byCode["P014"], IsGranted = false });
        await context.SaveChangesAsync();
        await PermissionSeeder.SeedAsync(context);
        (await context.UserPermissions.IgnoreQueryFilters()
                .FirstAsync(x => x.UserId == userId && x.PermissionId == byCode["P014"]))
            .IsGranted.Should().BeFalse();
    }

    private static IEnumerable<Kindi.API.Application.DTOs.responses.PermissionTreeNodeResponse> Flatten(
        IEnumerable<Kindi.API.Application.DTOs.responses.PermissionTreeNodeResponse> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            foreach (var child in Flatten(node.Children)) yield return child;
        }
    }

    private static PermissionService CreateService(ApplicationDbContext context)
        => new(
            new QueryService(context, new ReadContextStub(context)),
            new GenericRepository<RolePermission>(context, new UnitOfWorkStub(context)),
            new GenericRepository<UserPermission>(context, new UnitOfWorkStub(context)),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<PermissionService>.Instance);

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"kindi-permission-ancestor-{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options, new StubCurrentUserService());
    }

    private sealed class ReadContextStub(ApplicationDbContext context) : IReadDbContext
    {
        public DbSet<T> Set<T>() where T : class => context.Set<T>();
    }

    private sealed class UnitOfWorkStub(ApplicationDbContext context) : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => context.SaveChangesAsync(cancellationToken);

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task CommitTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task RollbackTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
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
