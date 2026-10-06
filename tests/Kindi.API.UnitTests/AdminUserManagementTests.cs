namespace Kindi.API.UnitTests;

using System.Globalization;
using FluentAssertions;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Infrastructure.Data;
using Kindi.API.Infrastructure.Repositories;
using Kindi.API.Infrastructure.Services;
using Kindi.API.Shared.Common.Interfaces;
using Kindi.API.Shared.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Xunit;

/// <summary>
/// Chốt nghiệp vụ quản lý người dùng ở màn quản trị: tạo tài khoản quản trị (mật khẩu đặt tay, chặn
/// trùng tên đăng nhập/email/SĐT kể cả bản ghi đã xoá mềm), sửa thông tin tập trung (được ghi đè hồ sơ),
/// xem chi tiết kèm hồ sơ CTV/đối tác liên kết, và bảo vệ tài khoản SuperAdmin.
/// </summary>
public class AdminUserManagementTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UserService _service;
    private readonly StubCurrentUserService _currentUser = new();
    private readonly RecordingAuthAuditService _authAudit = new();

    public AdminUserManagementTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"kindi-admin-users-{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options, _currentUser);
        var unitOfWork = new UnitOfWork(_context);

        _service = new UserService(
            new GenericRepository<User>(_context, unitOfWork),
            new GenericRepository<Collaborator>(_context, unitOfWork),
            new GenericRepository<Partner>(_context, unitOfWork),
            _currentUser,
            new NullLocalizer<Kindi.API.Application.Resources.SharedResource>(),
            new NullLocalizer<Kindi.API.Shared.Resources.ExceptionMessages>(),
            _authAudit);
    }

    public void Dispose() => _context.Dispose();

    private User SeedUser(string username, string? email = null, string phone = "0900000001",
        UserRole role = UserRole.User, bool isDeleted = false)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            FullName = "Nguyễn Văn A",
            Email = email ?? $"{username}@example.com",
            Phone = phone,
            Role = role,
            IsActive = true,
            IsDeleted = isDeleted
        };
        _context.Users.Add(user);
        _context.SaveChanges();
        return user;
    }

    private static CreateAdminUserRequest AdminRequest(string username = "ketoan.truong") => new()
    {
        Username = username,
        FullName = "Trần Thị B",
        Email = $"{username}@kindi.vn",
        Phone = "0912345678",
        Password = "Kindi@2026"
    };

    [Fact]
    public async Task Tao_tai_khoan_quan_tri_hop_le()
    {
        var result = await _service.CreateAdminAsync(AdminRequest());

        result.Role.Should().Be(nameof(UserRole.Admin));
        result.Username.Should().Be("ketoan.truong");
        result.UserCode.Should().StartWith("USR-");
        result.ReferralCode.Should().StartWith("CTV-");
        result.MustChangeCredentials.Should().BeFalse("mật khẩu do quản trị đặt nên không bắt đổi lần đầu");
        result.IsActive.Should().BeTrue();

        var saved = await _context.Users.SingleAsync(u => u.Username == "ketoan.truong");
        saved.Role.Should().Be(UserRole.Admin);
        BCrypt.Net.BCrypt.Verify("Kindi@2026", saved.PasswordHash).Should().BeTrue("mật khẩu phải được băm");
        _authAudit.Logs.Should().ContainSingle().Which.Should().Be((saved.Id, "ketoan.truong", "Register"));
    }

    [Fact]
    public async Task Tao_tai_khoan_quan_tri_chan_trung_ten_dang_nhap()
    {
        SeedUser("ketoan.truong", email: "khac@example.com", phone: "0900000009");

        await ((Func<Task>)(() => _service.CreateAdminAsync(AdminRequest())))
            .Should().ThrowAsync<AppException>().WithMessage("User_UsernameAlreadyExists");
    }

    [Fact]
    public async Task Tao_tai_khoan_quan_tri_chan_trung_email()
    {
        SeedUser("nguoidungkhac", email: "ketoan.truong@kindi.vn", phone: "0900000009");

        await ((Func<Task>)(() => _service.CreateAdminAsync(AdminRequest())))
            .Should().ThrowAsync<AppException>().WithMessage("User_EmailAlreadyExists");
    }

    [Fact]
    public async Task Tao_tai_khoan_quan_tri_chan_trung_sdt()
    {
        SeedUser("nguoidungkhac", email: "khac@example.com", phone: "0912345678");

        await ((Func<Task>)(() => _service.CreateAdminAsync(AdminRequest())))
            .Should().ThrowAsync<AppException>().WithMessage("User_PhoneAlreadyExists");
    }

    [Fact]
    public async Task Tao_tai_khoan_quan_tri_chan_trung_voi_tai_khoan_da_xoa_mem()
    {
        SeedUser("ketoan.truong", isDeleted: true);

        await ((Func<Task>)(() => _service.CreateAdminAsync(AdminRequest())))
            .Should().ThrowAsync<AppException>().WithMessage("User_UsernameAlreadyExists");
    }

    [Fact]
    public async Task Tao_tai_khoan_quan_tri_chan_mat_khau_qua_ngan_va_thieu_thong_tin()
    {
        var weakPassword = AdminRequest();
        weakPassword.Password = "1234567";
        await ((Func<Task>)(() => _service.CreateAdminAsync(weakPassword)))
            .Should().ThrowAsync<AppException>().WithMessage("User_PasswordTooWeak");

        var missingName = AdminRequest();
        missingName.FullName = "  ";
        await ((Func<Task>)(() => _service.CreateAdminAsync(missingName)))
            .Should().ThrowAsync<AppException>().WithMessage("User_FullNameRequired");

        var missingEmail = AdminRequest();
        missingEmail.Email = string.Empty;
        await ((Func<Task>)(() => _service.CreateAdminAsync(missingEmail)))
            .Should().ThrowAsync<AppException>().WithMessage("User_EmailRequired");
    }

    [Fact]
    public async Task Sua_thong_tin_nguoi_dung_ghi_de_duoc_ho_so_va_trang_thai()
    {
        var user = SeedUser("khachhang01", email: "cu@example.com", phone: "0900000001");

        var result = await _service.UpdateInfoAsync(user.Id, new UpdateUserInfoRequest
        {
            FullName = "Nguyễn Văn A (đã sửa)",
            Phone = "0987654321",
            Email = "moi@example.com",
            Zalo = "0987654321",
            IsActive = false
        });

        result!.FullName.Should().Be("Nguyễn Văn A (đã sửa)");
        result.Phone.Should().Be("0987654321");
        result.Email.Should().Be("moi@example.com");
        result.IsActive.Should().BeFalse();

        var saved = await _context.Users.SingleAsync(u => u.Id == user.Id);
        saved.Zalo.Should().Be("0987654321");
        saved.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Sua_thong_tin_chan_trung_sdt_voi_tai_khoan_khac()
    {
        SeedUser("nguoidungkhac", email: "khac@example.com", phone: "0911111111");
        var user = SeedUser("khachhang01", email: "cu@example.com", phone: "0900000001");

        var act = async () => await _service.UpdateInfoAsync(user.Id, new UpdateUserInfoRequest
        {
            FullName = "Nguyễn Văn A",
            Phone = "0911111111",
            Email = "cu@example.com"
        });

        await act.Should().ThrowAsync<AppException>().WithMessage("User_PhoneAlreadyExists");
    }

    [Fact]
    public async Task Chi_tiet_nguoi_dung_tra_kem_ho_so_ctv_va_doi_tac()
    {
        var user = SeedUser("congtacvien01");
        _context.Collaborators.Add(new Collaborator
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CollaboratorCode = "CTV-ABC123",
            ReferralCode = "CTV-ABC123",
            Position = "Nhân viên kinh doanh",
            BusinessName = "Công ty ABC",
            IsApproved = true,
            Level = 2
        });
        _context.Partners.Add(new Partner
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PartnerCode = "PART-260101-0001",
            Position = "Giám đốc",
            CompanyName = "Công ty ABC",
            Status = PartnerStatus.Approved
        });
        await _context.SaveChangesAsync();

        var detail = await _service.GetDetailAsync(user.Id);

        detail!.User.Username.Should().Be("congtacvien01");
        detail.Collaborator!.CollaboratorCode.Should().Be("CTV-ABC123");
        detail.Collaborator.IsApproved.Should().BeTrue();
        detail.Partner!.PartnerCode.Should().Be("PART-260101-0001");
        detail.Partner.Status.Should().Be(nameof(PartnerStatus.Approved));
    }

    [Fact]
    public async Task Chi_tiet_tra_null_khi_khong_co_ho_so_lien_ket()
    {
        var user = SeedUser("khongcolienket");

        var detail = await _service.GetDetailAsync(user.Id);

        detail!.Collaborator.Should().BeNull();
        detail.Partner.Should().BeNull();
    }

    [Fact]
    public async Task Gan_vai_tro_cho_tai_khoan_thuong_nhung_khong_gan_duoc_super_admin()
    {
        var user = SeedUser("nhanvien01");

        var result = await _service.UpdateRoleAsync(user.Id, UserRole.Partner);

        result!.Role.Should().Be(nameof(UserRole.Partner));
        (await _context.Users.SingleAsync(u => u.Id == user.Id)).Role.Should().Be(UserRole.Partner);

        await ((Func<Task>)(() => _service.UpdateRoleAsync(user.Id, UserRole.SuperAdmin)))
            .Should().ThrowAsync<AppException>().WithMessage("User_RoleNotAssignable");
    }

    [Fact]
    public async Task Tai_khoan_super_admin_khong_hien_va_khong_sua_duoc_voi_quan_tri_thuong()
    {
        var superAdmin = SeedUser("kindi_super", role: UserRole.SuperAdmin);
        _currentUser.Role = UserRole.Admin;

        (await _service.GetDetailAsync(superAdmin.Id)).Should().BeNull();
        (await _service.UpdateInfoAsync(superAdmin.Id, new UpdateUserInfoRequest { FullName = "Đổi trộm" }))
            .Should().BeNull();
        await ((Func<Task>)(() => _service.UpdateRoleAsync(superAdmin.Id, UserRole.User)))
            .Should().ThrowAsync<AppException>().WithMessage("User_SuperAdminImmutable");

        // Chính tài khoản SuperAdmin thì vẫn thấy được.
        _currentUser.Role = UserRole.SuperAdmin;
        (await _service.GetDetailAsync(superAdmin.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task Tab_tai_khoan_loc_dung_tai_khoan_thuong_va_tai_khoan_quan_tri()
    {
        SeedUser("khach01", email: "k1@example.com", phone: "0900000001", role: UserRole.User);
        SeedUser("ctv01", email: "k2@example.com", phone: "0900000002", role: UserRole.Partner);
        SeedUser("quantri01", email: "k3@example.com", phone: "0900000003", role: UserRole.Admin);
        SeedUser("kindi_super", email: "k4@example.com", phone: "0900000004", role: UserRole.SuperAdmin);

        var admins = await _service.GetPagedAsync(new UserQueryDto
        {
            Scope = UserAccountScope.Admin,
            PageNumber = 1,
            PageSize = 20
        });
        admins.Items.Select(x => x.Username).Should().Equal("quantri01");

        var customers = await _service.GetPagedAsync(new UserQueryDto
        {
            Scope = UserAccountScope.Customer,
            PageNumber = 1,
            PageSize = 20
        });
        customers.Items.Select(x => x.Username).Should().BeEquivalentTo(new[] { "khach01", "ctv01" });
    }

    private sealed class StubCurrentUserService : ICurrentUserService
    {
        public UserRole Role { get; set; } = UserRole.Admin;

        public string? UserId { get; set; } = Guid.NewGuid().ToString();
        public string? UserName => "admin";
        public bool IsAuthenticated => true;
        public bool IsInRole(string role)
            => role == ((int)Role).ToString(CultureInfo.InvariantCulture)
               || role.Equals(Role.ToString(), StringComparison.OrdinalIgnoreCase);
        public string? IpAddress => null;
        public string? UserAgent => null;
    }

    private sealed class RecordingAuthAuditService : IAuthAuditService
    {
        public List<(Guid? UserId, string? Username, string Action)> Logs { get; } = new();

        public Task LogAsync(Guid? userId, string? username, string action, bool isSuccess, string? detail = null,
            CancellationToken cancellationToken = default)
        {
            Logs.Add((userId, username, action));
            return Task.CompletedTask;
        }
    }

    private sealed class NullLocalizer<T> : IStringLocalizer<T>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: true);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.InvariantCulture, name, arguments), resourceNotFound: true);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }
}
