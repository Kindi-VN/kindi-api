namespace Kindi.API.UnitTests;

using System.Globalization;
using System.Linq.Expressions;
using AutoMapper;
using FluentAssertions;
using Kindi.API.Application.Common.Helpers;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Application.Mappings;
using Kindi.API.Application.Services;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Models;
using Kindi.API.Infrastructure.Data;
using Kindi.API.Infrastructure.Repositories;
using Kindi.API.Shared.Common.Interfaces;
using Kindi.API.Shared.Constants;
using Kindi.API.Shared.Errors;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Chốt nghiệp vụ xoá nhật ký hoạt động (trang Nhật ký trong Cài đặt): xoá theo dòng được chọn, xoá
/// theo khoảng ngày (phủ HẾT ngày kết thúc), bắt buộc có điều kiện, không đụng nhật ký của SuperAdmin
/// khi đang ở chế độ xem thường, và luôn để lại một dòng nhật ký ghi vết thao tác xoá.
/// </summary>
public class AuditLogDeletionTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AuditLogQueryService _service;
    private readonly StubCurrentUserService _currentUserService = new()
    {
        UserId = Guid.NewGuid().ToString(),
        UserName = "admin",
        IpAddress = "14.161.15.186"
    };

    private readonly Guid _superAdminId = Guid.NewGuid();

    public AuditLogDeletionTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"kindi-audit-delete-{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options, _currentUserService);
        var unitOfWork = new UnitOfWork(_context);
        _service = new AuditLogQueryService(
            new DbContextQueryService(_context),
            new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper(),
            new GenericRepository<AuditLog>(_context, unitOfWork),
            new GenericRepository<AuthAuditLog>(_context, unitOfWork),
            _currentUserService);
    }

    public void Dispose() => _context.Dispose();

    private AuditLog SeedEntityLog(string entityName, DateTime timestamp, string? actorId = null)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = entityName,
            EntityId = Guid.NewGuid(),
            Action = AuditAction.Update,
            ActorId = actorId,
            ActorName = actorId == null ? "khach" : "quantri",
            Timestamp = timestamp
        };
        _context.AuditLogs.Add(log);
        _context.SaveChanges();
        return log;
    }

    private static DateTime Utc(int year, int month, int day, int hour = 0)
        => new(year, month, day, hour, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Xoa_theo_lua_chon_chi_xoa_dung_cac_dong_duoc_chon_va_ghi_vet()
    {
        var keep = SeedEntityLog("Partner", Utc(2026, 10, 1));
        var first = SeedEntityLog("Partner", Utc(2026, 10, 2));
        var second = SeedEntityLog("PurchaseRequest", Utc(2026, 10, 3));

        var deleted = await _service.DeleteEntityLogsAsync(new AuditLogDeleteRequest
        {
            Ids = new List<Guid> { first.Id, second.Id }
        });

        deleted.Should().Be(2);
        // Bỏ dòng ghi vết của chính thao tác xoá, chỉ so các dòng nhật ký còn lại.
        var remaining = await _context.AuditLogs
            .Where(x => x.Action != AuditAction.Delete)
            .Select(x => x.Id)
            .ToListAsync();
        remaining.Should().BeEquivalentTo(new[] { keep.Id });

        // Dòng ghi vết: biết ai xoá, xoá theo điều kiện nào và mất bao nhiêu dòng.
        var trace = await _context.AuditLogs.SingleAsync(x => x.Action == AuditAction.Delete);
        trace.EntityName.Should().Be("AuditLog");
        trace.ActorId.Should().Be(_currentUserService.UserId);
        trace.ActorName.Should().Be("admin");
        trace.IpAddress.Should().Be("14.161.15.186");
        trace.OldValues.Should().Contain("selection").And.Contain("\"deletedCount\":2");
    }

    [Fact]
    public async Task Xoa_theo_khoang_ngay_phu_het_ngay_ket_thuc()
    {
        SeedEntityLog("Partner", Utc(2026, 10, 4, 23));
        SeedEntityLog("Partner", Utc(2026, 10, 5, 8));
        var onEndDate = SeedEntityLog("Partner", Utc(2026, 10, 6, 14));
        var afterEndDate = SeedEntityLog("Partner", Utc(2026, 10, 7, 1));

        var deleted = await _service.DeleteEntityLogsAsync(new AuditLogDeleteRequest
        {
            FromDate = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc),
            ToDate = new DateTime(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc) // chỉ gửi NGÀY kết thúc
        });

        deleted.Should().Be(2, "ngày kết thúc phải được tính trọn ngày");
        var remaining = await _context.AuditLogs
            .Where(x => x.Action != AuditAction.Delete)
            .Select(x => x.Id)
            .ToListAsync();
        remaining.Should().Contain(afterEndDate.Id);
        remaining.Should().NotContain(onEndDate.Id);
        remaining.Should().HaveCount(2);
    }

    [Fact]
    public async Task Xoa_ma_khong_co_dieu_kien_thi_bi_chan()
    {
        SeedEntityLog("Partner", Utc(2026, 10, 6));

        var act = async () => await _service.DeleteEntityLogsAsync(new AuditLogDeleteRequest());

        await act.Should().ThrowAsync<AppException>()
            .WithMessage("AuditLog_DeleteCriteriaRequired");
        (await _context.AuditLogs.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Khoang_ngay_nguoc_thu_tu_thi_bao_loi()
    {
        var act = async () => await _service.DeleteEntityLogsAsync(new AuditLogDeleteRequest
        {
            FromDate = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc),
            ToDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        await act.Should().ThrowAsync<AppException>()
            .WithMessage("AuditLog_DeleteRangeInvalid");
    }

    [Fact]
    public async Task Che_do_xem_thuong_khong_xoa_duoc_nhat_ky_cua_super_admin()
    {
        await SeedSuperAdminUserAsync();
        var superAdminLog = SeedEntityLog("Partner", Utc(2026, 10, 6, 9), _superAdminId.ToString());
        var adminLog = SeedEntityLog("Partner", Utc(2026, 10, 6, 10), _currentUserService.UserId);

        var deleted = await _service.DeleteEntityLogsAsync(new AuditLogDeleteRequest
        {
            FromDate = new DateTime(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc)
        });

        deleted.Should().Be(1);
        (await _context.AuditLogs.AnyAsync(x => x.Id == superAdminLog.Id)).Should().BeTrue(
            "admin thường không thấy thì cũng không xoá được nhật ký của SuperAdmin");
        (await _context.AuditLogs.AnyAsync(x => x.Id == adminLog.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task Che_do_xem_toan_bo_thi_xoa_duoc_ca_nhat_ky_super_admin()
    {
        await SeedSuperAdminUserAsync();
        var superAdminLog = SeedEntityLog("Partner", Utc(2026, 10, 6, 9), _superAdminId.ToString());

        var deleted = await _service.DeleteEntityLogsAsync(new AuditLogDeleteRequest
        {
            Ids = new List<Guid> { superAdminLog.Id }
        }, includeSuperAdminActors: true);

        deleted.Should().Be(1);
        (await _context.AuditLogs.AnyAsync(x => x.Id == superAdminLog.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task Xoa_nhat_ky_xac_thuc_theo_khoang_ngay()
    {
        _context.AuthAuditLogs.AddRange(
            new AuthAuditLog { Id = Guid.NewGuid(), Username = "a", Action = AuditAction.Login, IsSuccess = true, Timestamp = Utc(2026, 10, 5, 7) },
            new AuthAuditLog { Id = Guid.NewGuid(), Username = "b", Action = AuditAction.Register, IsSuccess = true, Timestamp = Utc(2026, 10, 6, 7) },
            new AuthAuditLog { Id = Guid.NewGuid(), Username = "c", Action = AuditAction.Login, IsSuccess = true, Timestamp = Utc(2026, 10, 9, 7) });
        await _context.SaveChangesAsync();

        var deleted = await _service.DeleteAuthLogsAsync(new AuditLogDeleteRequest
        {
            FromDate = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc),
            ToDate = new DateTime(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc)
        });

        deleted.Should().Be(2);
        (await _context.AuthAuditLogs.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Danh_muc_loc_lay_ten_bang_tu_du_lieu_va_bo_trung()
    {
        SeedEntityLog("Partner", Utc(2026, 10, 1));
        SeedEntityLog("Partner", Utc(2026, 10, 2));
        SeedEntityLog("PurchaseRequest", Utc(2026, 10, 3));

        var options = await _service.GetFilterOptionsAsync();

        options.EntityNames.Should().BeEquivalentTo(new[] { "Partner", "PurchaseRequest" });
        options.EntityActions.Should().BeEquivalentTo(new[] { AuditAction.Create, AuditAction.Update, AuditAction.Delete });
        options.AuthActions.Should().Contain(new[] { AuditAction.Login, AuditAction.Register, AuditAction.ResetPassword });
    }

    [Fact]
    public void Danh_muc_loc_bo_gia_tri_rong_va_sap_xep()
    {
        var options = AuditLogFilterOptionsResponse.Build(new[] { " Partner ", "", "purchaseRequest", "PARTNER", "   " });

        options.EntityNames.Should().Equal("Partner", "purchaseRequest");
    }

    private async Task SeedSuperAdminUserAsync()
    {
        _context.Users.Add(new User
        {
            Id = _superAdminId,
            Username = "kindi_super",
            FullName = "Quản trị tối cao",
            Email = "kindi_super@kindi.vn",
            Role = UserRole.SuperAdmin
        });
        await _context.SaveChangesAsync();

        // Tạo tài khoản cũng sinh 1 dòng nhật ký (User/Create) — dọn để mỗi test bắt đầu từ trạng thái sạch.
        _context.AuditLogs.RemoveRange(_context.AuditLogs.ToList());
        await _context.SaveChangesAsync();
    }

    private sealed class StubCurrentUserService : ICurrentUserService
    {
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public bool IsAuthenticated => UserId != null;
        public bool IsInRole(string role) => false;
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }

    /// <summary>IQueryService đọc thẳng từ DbContext InMemory của test (đủ cho phần đọc dữ liệu test cần).</summary>
    private sealed class DbContextQueryService(ApplicationDbContext context) : IQueryService
    {
        public IQueryable<T> GetQueryable<T>() where T : class => context.Set<T>().AsQueryable();
        public IQueryable<T> GetQueryableNoTracking<T>() where T : class => context.Set<T>().AsNoTracking();
        public IQueryable<T> GetAll<T>() where T : class => context.Set<T>().AsQueryable();
        public IQueryable<T> GetAllNoTracking<T>() where T : class => context.Set<T>().AsNoTracking();

        public Task<T?> GetByIdAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : class
            => context.Set<T>().FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);

        public Task<List<T>> GetListAsync<T>(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default) where T : class
            => (predicate == null ? context.Set<T>() : context.Set<T>().Where(predicate)).ToListAsync(cancellationToken);

        public Task<T?> GetFirstOrDefaultAsync<T>(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default) where T : class
            => (predicate == null ? context.Set<T>() : context.Set<T>().Where(predicate)).FirstOrDefaultAsync(cancellationToken);

        public Task<T?> GetFirstOrDefaultAsync<T>(Expression<Func<T, bool>>? predicate, Func<IQueryable<T>, IQueryable<T>>? sortFunc, CancellationToken cancellationToken = default) where T : class
            => (sortFunc ?? (query => query))(context.Set<T>()).FirstOrDefaultAsync(cancellationToken);

        public Task<T?> GetLastOrDefaultAsync<T, TKey>(Expression<Func<T, bool>>? predicate, Expression<Func<T, TKey>> orderBy, bool descending = true, CancellationToken cancellationToken = default) where T : class
            => throw new NotImplementedException();

        public Task<bool> AnyAsync<T>(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default) where T : class
            => (predicate == null ? context.Set<T>() : context.Set<T>().Where(predicate)).AnyAsync(cancellationToken);

        public Task<int> CountAsync<T>(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default) where T : class
            => (predicate == null ? context.Set<T>() : context.Set<T>().Where(predicate)).CountAsync(cancellationToken);

        public Task<PagedList<T>> GetPagedListAsync<T>(int pageNumber, int pageSize, Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default) where T : class
            => throw new NotImplementedException();

        public Task<PagedList<T>> GetPagedListAsync<T>(int pageNumber, int pageSize, Expression<Func<T, bool>>? predicate, string? sortBy, string? sortOrder = null, string? defaultSortBy = null, CancellationToken cancellationToken = default) where T : class
            => throw new NotImplementedException();

        public Task<PagedList<T>> GetPagedListAsync<T>(int pageNumber, int pageSize, Expression<Func<T, bool>>? predicate, Func<IQueryable<T>, IQueryable<T>>? sortFunc, CancellationToken cancellationToken = default) where T : class
            => throw new NotImplementedException();
    }
}
