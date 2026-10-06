namespace Kindi.API.UnitTests;

using System.Globalization;
using System.Linq.Expressions;
using AutoMapper;
using FluentAssertions;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.Common.Models;
using Kindi.API.Application.DTOs.Requests;
using Kindi.API.Application.DTOs.Responses;
using Kindi.API.Application.Mappings;
using Kindi.API.Application.Services;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Domain.Models;
using Kindi.API.Infrastructure.Services;
using Kindi.API.Shared.Common.Interfaces;
using Microsoft.Extensions.Localization;
using Xunit;

/// <summary>
/// Chốt quy tắc ghi hồ sơ cá nhân: bảng Users là nơi lưu DUY NHẤT, nhưng luồng form công khai
/// (<c>allowContactChange: false</c>) chỉ được ĐIỀN CHỖ TRỐNG — không ghi đè họ tên/SĐT/email/Zalo
/// của tài khoản đã có, kể cả khi người gửi đang đăng nhập. Muốn đổi hồ sơ phải qua endpoint của
/// chính chủ hồ sơ (<c>PUT /Auth/me</c>) hoặc endpoint quản trị (truyền allowContactChange: true).
/// </summary>
public class PublicFormPersonalInfoTests
{
    private const string AdminPhone = "0987654321";
    private const string AdminEmail = "admin@flashoffer.com";

    private static IMapper CreateMapper()
        => new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

    private static UserService CreateService(FakeUserRepository repository)
        => new(repository, new StubCurrentUserService(),
            new NullLocalizer<Kindi.API.Application.Resources.SharedResource>(),
            new NullLocalizer<Kindi.API.Shared.Resources.ExceptionMessages>(), new StubAuthAuditService());

    private static User AdminUser() => new()
    {
        Id = Guid.NewGuid(),
        Username = "admin",
        FullName = "Chị Chi",
        Phone = AdminPhone,
        Email = AdminEmail,
        Zalo = "0900000000",
        Role = UserRole.Admin,
        IsActive = true
    };

    [Fact]
    public async Task Form_cong_khai_khong_ghi_de_ho_ten_va_zalo_cua_tai_khoan_da_co()
    {
        var admin = AdminUser();
        var repository = new FakeUserRepository(admin);

        await CreateService(repository).UpdatePersonalInfoAsync(
            admin.Id, "dgdfgdfg", AdminPhone, AdminEmail, "0911111111");

        admin.FullName.Should().Be("Chị Chi", "form công khai không được đổi họ tên tài khoản đã có");
        admin.Zalo.Should().Be("0900000000", "form công khai không được đổi Zalo tài khoản đã có");
        admin.Phone.Should().Be(AdminPhone);
        admin.Email.Should().Be(AdminEmail);
        repository.SavedChanges.Should().Be(0, "không có gì đổi thì không ghi DB");
    }

    [Fact]
    public async Task Form_cong_khai_dien_ho_ten_va_zalo_khi_ho_so_con_trong()
    {
        var user = new User { Id = Guid.NewGuid(), Username = "user0900000000", FullName = "  ", Email = "a@x.com" };
        var repository = new FakeUserRepository(user);

        await CreateService(repository).UpdatePersonalInfoAsync(user.Id, "Nguyễn Văn A", "0900000000", null, " 0900000001 ");

        user.FullName.Should().Be("Nguyễn Văn A");
        user.Zalo.Should().Be("0900000001");
        repository.SavedChanges.Should().Be(1);
    }

    [Fact]
    public async Task Chu_ho_so_hoac_quan_tri_duoc_ghi_de_ho_ten()
    {
        var admin = AdminUser();
        var repository = new FakeUserRepository(admin);

        await CreateService(repository).UpdatePersonalInfoAsync(
            admin.Id, "Hà Thu Shinhan", null, null, null, allowContactChange: true);

        admin.FullName.Should().Be("Hà Thu Shinhan");
    }

    [Fact]
    public async Task Khach_gui_form_bang_sdt_cua_tai_khoan_da_co_thi_khong_doi_ten_ho()
    {
        var admin = AdminUser();
        var repository = new FakeUserRepository(admin);

        var resolved = await CreateService(repository).ResolvePublicUserAsync("Kẻ lạ", AdminPhone, AdminEmail, null);

        resolved.UserId.Should().Be(admin.Id, "dùng lại tài khoản theo SĐT/email đã có");
        resolved.IsNewAccount.Should().BeFalse();
        admin.FullName.Should().Be("Chị Chi", "người khác nhập SĐT/email của tài khoản đã có không được đổi hồ sơ");
    }

    [Fact]
    public async Task Khach_gui_form_bang_sdt_moi_thi_tao_tai_khoan_voi_thong_tin_trong_form()
    {
        var repository = new FakeUserRepository();

        var resolved = await CreateService(repository).ResolvePublicUserAsync("Nguyễn Văn A", "0912345678", "a@x.com", "0912345678");

        resolved.IsNewAccount.Should().BeTrue();
        var created = repository.Items.Should().ContainSingle().Subject;
        created.FullName.Should().Be("Nguyễn Văn A");
        created.Phone.Should().Be("0912345678");
        created.Zalo.Should().Be("0912345678");
        created.MustChangeCredentials.Should().BeTrue();
    }

    [Fact]
    public async Task Form_dang_ky_doi_tac_gui_khi_dang_dang_nhap_khong_doi_ten_tai_khoan()
    {
        var admin = AdminUser();
        var userRepository = new FakeUserRepository(admin);
        var partnerRepository = new FakeRepository<Partner>();

        var service = new PartnerService(
            partnerRepository,
            CreateService(userRepository),
            new StubCurrentUserService(admin.Id),
            CreateMapper(),
            new NullLocalizer<Kindi.API.Application.Resources.SharedResource>(),
            new StubReferralService(),
            new StubQueryService(),
            new FakeRepository<PartnerProduct>(),
            new FakeRepository<BusinessField>(),
            new StubCompanyService());

        var response = await service.RegisterAsync(new PartnerRegisterRequest
        {
            FullName = "dgdfgdfg",
            Email = AdminEmail,
            Phone = AdminPhone,
            Position = "đfgdgd",
            CompanyName = "fdgdfd",
            CompanyAddress = "Bắc Giang",
            CompanySize = CompanySize.Size51_200,
            AgreeTerms = true
        });

        response.Should().NotBeNull();
        partnerRepository.Items.Should().ContainSingle()
            .Which.UserId.Should().Be(admin.Id, "người đã đăng nhập thì hồ sơ đối tác gắn vào chính tài khoản đó");
        admin.FullName.Should().Be("Chị Chi", "form đăng ký đối tác không được ghi đè họ tên tài khoản đã có");
    }

    private sealed class FakeUserRepository(params User[] seed) : IRepository<User>
    {
        public List<User> Items { get; } = seed.ToList();
        public int SavedChanges { get; private set; }

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.FirstOrDefault(u => u.Id == id));

        public Task<User?> GetFirstAsync(Expression<Func<User, bool>> predicate, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.AsQueryable().FirstOrDefault(predicate));

        public IQueryable<User> GetQueryable() => Items.AsQueryable();

        public Task AddAsync(User entity, CancellationToken cancellationToken = default)
        {
            Items.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(User entity) { }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(++SavedChanges);

        public Task<User?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<User>> FindAsync(Expression<Func<User, bool>> predicate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IQueryable<User>> GetQueryableAsync() => throw new NotImplementedException();
        public Task<PagedList<User>> GetPagedAsync(int pageNumber, int pageSize, Expression<Func<User, bool>>? predicate = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PagedList<User>> GetPagedWithOrderAsync(int pageNumber, int pageSize, Expression<Func<User, bool>>? predicate, Expression<Func<User, object>>? orderBy, bool isDescending = true, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PagedList<User>> GetPagedWithIncludesAsync(int pageNumber, int pageSize, Func<IQueryable<User>, IQueryable<User>>? includes = null, Expression<Func<User, bool>>? predicate = null, Expression<Func<User, object>>? orderBy = null, bool isDescending = true, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> CountAsync(Expression<Func<User, bool>>? predicate = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<User?> GetFirstWithIncludesAsync(Expression<Func<User, bool>> predicate, Func<IQueryable<User>, IQueryable<User>>? includes = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<User>> GetListWithIncludesAsync(Func<IQueryable<User>, IQueryable<User>>? includes = null, Expression<Func<User, bool>>? predicate = null, Expression<Func<User, object>>? orderBy = null, bool isDescending = true, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> AnyAsync(Expression<Func<User, bool>> predicate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<User>> FromSqlRawAsync(string sql, params object[] parameters) => throw new NotImplementedException();
        public Task<IEnumerable<User>> GetDeletedAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddRangeAsync(IEnumerable<User> entities, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public void UpdateRange(IEnumerable<User> entities) => throw new NotImplementedException();
        public void Delete(User entity) => throw new NotImplementedException();
        public void DeleteRange(IEnumerable<User> entities) => throw new NotImplementedException();
        public void Restore(User entity) => throw new NotImplementedException();
        public void RestoreRange(IEnumerable<User> entities) => throw new NotImplementedException();
    }

    private sealed class FakeRepository<T> : IRepository<T> where T : class
    {
        public List<T> Items { get; } = new();
        public int SavedChanges { get; private set; }

        public Task AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            Items.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(T entity) { }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(++SavedChanges);

        public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<T?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<T?> GetFirstAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IQueryable<T> GetQueryable() => throw new NotImplementedException();
        public Task<IQueryable<T>> GetQueryableAsync() => throw new NotImplementedException();
        public Task<PagedList<T>> GetPagedAsync(int pageNumber, int pageSize, Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PagedList<T>> GetPagedWithOrderAsync(int pageNumber, int pageSize, Expression<Func<T, bool>>? predicate, Expression<Func<T, object>>? orderBy, bool isDescending = true, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PagedList<T>> GetPagedWithIncludesAsync(int pageNumber, int pageSize, Func<IQueryable<T>, IQueryable<T>>? includes = null, Expression<Func<T, bool>>? predicate = null, Expression<Func<T, object>>? orderBy = null, bool isDescending = true, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<T?> GetFirstWithIncludesAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IQueryable<T>>? includes = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<T>> GetListWithIncludesAsync(Func<IQueryable<T>, IQueryable<T>>? includes = null, Expression<Func<T, bool>>? predicate = null, Expression<Func<T, object>>? orderBy = null, bool isDescending = true, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<T>> FromSqlRawAsync(string sql, params object[] parameters) => throw new NotImplementedException();
        public Task<IEnumerable<T>> GetDeletedAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public void UpdateRange(IEnumerable<T> entities) => throw new NotImplementedException();
        public void Delete(T entity) => throw new NotImplementedException();
        public void DeleteRange(IEnumerable<T> entities) => throw new NotImplementedException();
        public void Restore(T entity) => throw new NotImplementedException();
        public void RestoreRange(IEnumerable<T> entities) => throw new NotImplementedException();
    }

    private sealed class StubReferralService : IReferralService
    {
        public Task<string?> ResolveForUserAsync(Guid userId, string? referralCode) => Task.FromResult<string?>(null);

        public Task RecordEventAsync(string? referralCode, Guid referredUserId, ReferralEventType eventType,
            Guid? refEntityId = null, string? refEntityCode = null, decimal? amount = null, bool isGuestAccount = false)
            => Task.CompletedTask;

        public Task<string?> GetSharerReferralCodeAsync() => throw new NotImplementedException();
        public Task<string?> ResolveAsync(string? referralCode) => throw new NotImplementedException();
        public Task<string?> AttributeToCurrentUserAsync(string? referralCode) => throw new NotImplementedException();
        public Task SetEventStatusAsync(ReferralEventType eventType, Guid refEntityId, ReferralEventStatus? status) => throw new NotImplementedException();
        public Task SetEventStatusAsync(ReferralEventType eventType, IEnumerable<Guid> refEntityIds, ReferralEventStatus? status) => throw new NotImplementedException();
        public Task<Dictionary<string, string>> LoadNamesAsync(IEnumerable<string?> referralCodes) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<string>> FindReferrerCodesByNameAsync(string searchTerm, bool unaccentAndCaseInsensitive) => throw new NotImplementedException();
        public Task FillNamesAsync<T>(IEnumerable<T> items, Func<T, string?> getReferralCode, Action<T, string> setReferralName) => throw new NotImplementedException();
    }

    private sealed class StubCompanyService : ICompanyService
    {
        public Task<Company?> AddOrUpdateFromLegacyAsync(string? name, string? tax, string? address, string? website,
            Guid? businessFieldId, BusinessType? businessType, CompanySize? companySize)
            => Task.FromResult<Company?>(new Company { Id = Guid.NewGuid(), Name = name ?? string.Empty });

        public Task<CompanyResponseDto> CreateAsync(CreateCompanyDto request) => throw new NotImplementedException();
        public Task<CompanyResponseDto> UpdateAsync(Guid id, UpdateCompanyDto request) => throw new NotImplementedException();
        public Task<PagedList<CompanyResponseDto>> GetPagedAsync(int pageNumber, int pageSize, string? search = null) => throw new NotImplementedException();
        public Task<CompanyResponseDto?> GetByIdAsync(Guid id) => throw new NotImplementedException();
    }

    private sealed class StubQueryService : IQueryService
    {
        public IQueryable<T> GetQueryable<T>() where T : class => throw new NotImplementedException();
        public IQueryable<T> GetQueryableNoTracking<T>() where T : class => throw new NotImplementedException();
        public IQueryable<T> GetAll<T>() where T : class => throw new NotImplementedException();
        public IQueryable<T> GetAllNoTracking<T>() where T : class => throw new NotImplementedException();
        public Task<T?> GetByIdAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : class => throw new NotImplementedException();
        public Task<List<T>> GetListAsync<T>(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default) where T : class => throw new NotImplementedException();
        public Task<T?> GetFirstOrDefaultAsync<T>(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default) where T : class => throw new NotImplementedException();
        public Task<T?> GetFirstOrDefaultAsync<T>(Expression<Func<T, bool>>? predicate, Func<IQueryable<T>, IQueryable<T>>? sortFunc, CancellationToken cancellationToken = default) where T : class => throw new NotImplementedException();
        public Task<T?> GetLastOrDefaultAsync<T, TKey>(Expression<Func<T, bool>>? predicate, Expression<Func<T, TKey>> orderBy, bool descending = true, CancellationToken cancellationToken = default) where T : class => throw new NotImplementedException();
        public Task<bool> AnyAsync<T>(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default) where T : class => throw new NotImplementedException();
        public Task<int> CountAsync<T>(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default) where T : class => throw new NotImplementedException();
        public Task<PagedList<T>> GetPagedListAsync<T>(int pageNumber, int pageSize, Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default) where T : class => throw new NotImplementedException();
        public Task<PagedList<T>> GetPagedListAsync<T>(int pageNumber, int pageSize, Expression<Func<T, bool>>? predicate, string? sortBy, string? sortOrder = null, string? defaultSortBy = null, CancellationToken cancellationToken = default) where T : class => throw new NotImplementedException();
        public Task<PagedList<T>> GetPagedListAsync<T>(int pageNumber, int pageSize, Expression<Func<T, bool>>? predicate, Func<IQueryable<T>, IQueryable<T>>? sortFunc, CancellationToken cancellationToken = default) where T : class => throw new NotImplementedException();
    }

    private sealed class StubCurrentUserService : ICurrentUserService
    {
        public StubCurrentUserService(Guid? userId = null) => UserId = userId?.ToString();

        public string? UserId { get; }
        public string? UserName => null;
        public bool IsAuthenticated => UserId != null;
        public bool IsInRole(string role) => false;
        public string? IpAddress => null;
        public string? UserAgent => null;
    }

    private sealed class StubAuthAuditService : IAuthAuditService
    {
        public Task LogAsync(Guid? userId, string? username, string action, bool isSuccess, string? detail = null,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NullLocalizer<T> : IStringLocalizer<T>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: true);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.InvariantCulture, name, arguments), resourceNotFound: true);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }
}
