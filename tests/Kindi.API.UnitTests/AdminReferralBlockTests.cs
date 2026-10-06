namespace Kindi.API.UnitTests;

using System.Linq.Expressions;
using FluentAssertions;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.Services;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Domain.Models;
using Kindi.API.Shared.Common.Interfaces;
using Xunit;

/// <summary>
/// Chốt quy tắc: tài khoản quản trị KHÔNG được gắn vào bất cứ dạng tài khoản nào — không nhận ghi nhận giới
/// thiệu (tài khoản thường/CTV/đối tác/yêu cầu...) và mã chia sẻ của tài khoản quản trị cũng không dùng để
/// giới thiệu ai. Mọi luồng đều đi qua <see cref="ReferralService.ResolveForUserAsync"/> nên chốt ở đây là
/// chốt cho tất cả.
/// </summary>
public class AdminReferralBlockTests
{
    private const string CollaboratorCode = "CTV-ABC123";

    private static (ReferralService Service, FakeRepository<User> Users, FakeRepository<ReferralEvent> Events)
        BuildService(User? seedUser = null, bool seedCollaboratorCode = true)
    {
        var users = new FakeRepository<User>();
        var events = new FakeRepository<ReferralEvent>();
        var collaborators = new FakeRepository<Collaborator>();
        var queries = new FakeQueryService();

        if (seedUser != null)
        {
            users.Items.Add(seedUser);
            queries.Users.Add(seedUser);
        }

        if (seedCollaboratorCode)
        {
            collaborators.Items.Add(new Collaborator { ReferralCode = CollaboratorCode });
            queries.Collaborators.Add(new Collaborator { ReferralCode = CollaboratorCode });
        }

        var service = new ReferralService(collaborators, users, events, new StubCurrentUserService(), queries);
        return (service, users, events);
    }

    private static User BuildUser(UserRole role, string? referralCode = null) => new()
    {
        Username = role == UserRole.User ? "khach01" : "quantri01",
        FullName = "Người dùng thử",
        Role = role,
        ReferralCode = referralCode
    };

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.SuperAdmin)]
    public async Task Tai_khoan_quan_tri_khong_nhan_ghi_nhan_gioi_thieu(UserRole role)
    {
        var admin = BuildUser(role);
        var (service, users, events) = BuildService(admin);

        var resolved = await service.ResolveForUserAsync(admin.Id, CollaboratorCode);

        resolved.Should().BeNull();
        users.Items.Single(u => u.Id == admin.Id).AccountReferrerCode.Should().BeNull();
        users.Items.Single(u => u.Id == admin.Id).AccountReferrerAt.Should().BeNull();
        events.Items.Should().BeEmpty("không được ghi sự kiện giới thiệu cho tài khoản quản trị");
    }

    [Fact]
    public async Task Ma_chia_se_cua_tai_khoan_quan_tri_khong_dung_de_gioi_thieu()
    {
        var admin = BuildUser(UserRole.Admin, "ADMIN-CODE");
        var (service, _, _) = BuildService(admin);

        (await service.ResolveAsync("ADMIN-CODE")).Should().BeNull("mã chia sẻ của tài khoản quản trị không dùng để giới thiệu");
        (await service.ResolveAsync(CollaboratorCode)).Should().Be(CollaboratorCode, "mã của CTV vẫn dùng bình thường");
    }

    [Fact]
    public async Task Tai_khoan_thuong_van_duoc_ghi_nhan_gioi_thieu_binh_thuong()
    {
        var customer = BuildUser(UserRole.User);
        var (service, users, events) = BuildService(customer);

        var resolved = await service.ResolveForUserAsync(customer.Id, CollaboratorCode);

        resolved.Should().Be(CollaboratorCode);
        users.Items.Single().AccountReferrerCode.Should().Be(CollaboratorCode);
        users.Items.Single().AccountReferrerAt.Should().NotBeNull();
        events.Items.Should().ContainSingle(e => e.EventType == ReferralEventType.UserReferred);
    }

    private sealed class FakeRepository<T> : IRepository<T> where T : BaseEntity
    {
        public List<T> Items { get; } = new();
        public int SavedChanges { get; private set; }

        public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.FirstOrDefault(item => item.Id == id));

        public Task<T?> GetFirstAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.AsQueryable().FirstOrDefault(predicate));

        public IQueryable<T> GetQueryable() => Items.AsQueryable();

        public Task AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            Items.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(T entity) { }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(++SavedChanges);

        public Task<T?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
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
        public void HardDelete(T entity) => throw new NotImplementedException();
        public void HardDeleteRange(IEnumerable<T> entities) => throw new NotImplementedException();
        public Task<int> HardDeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    /// <summary>IQueryService đủ dùng cho luồng tra mã chia sẻ (AnyAsync trên Users/Collaborators).</summary>
    private sealed class FakeQueryService : IQueryService
    {
        public List<User> Users { get; } = new();
        public List<Collaborator> Collaborators { get; } = new();

        private IEnumerable<T> Source<T>() where T : class
            => typeof(T) == typeof(User) ? Users.Cast<T>()
            : typeof(T) == typeof(Collaborator) ? Collaborators.Cast<T>()
            : Enumerable.Empty<T>();

        public Task<bool> AnyAsync<T>(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default) where T : class
        {
            var source = Source<T>().AsQueryable();
            return Task.FromResult(predicate == null ? source.Any() : source.Any(predicate));
        }

        public Task<T?> GetFirstOrDefaultAsync<T>(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default) where T : class
            => Task.FromResult(predicate == null ? Source<T>().FirstOrDefault() : Source<T>().AsQueryable().FirstOrDefault(predicate));

        public IQueryable<T> GetQueryable<T>() where T : class => Source<T>().AsQueryable();
        public IQueryable<T> GetQueryableNoTracking<T>() where T : class => throw new NotImplementedException();
        public IQueryable<T> GetAll<T>() where T : class => throw new NotImplementedException();
        public IQueryable<T> GetAllNoTracking<T>() where T : class => throw new NotImplementedException();
        public Task<T?> GetByIdAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : class => throw new NotImplementedException();
        public Task<List<T>> GetListAsync<T>(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default) where T : class => throw new NotImplementedException();
        public Task<T?> GetFirstOrDefaultAsync<T>(Expression<Func<T, bool>>? predicate, Func<IQueryable<T>, IQueryable<T>>? sortFunc, CancellationToken cancellationToken = default) where T : class => throw new NotImplementedException();
        public Task<T?> GetLastOrDefaultAsync<T, TKey>(Expression<Func<T, bool>>? predicate, Expression<Func<T, TKey>> orderBy, bool descending = true, CancellationToken cancellationToken = default) where T : class => throw new NotImplementedException();
        public Task<int> CountAsync<T>(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default) where T : class => throw new NotImplementedException();
        public Task<PagedList<T>> GetPagedListAsync<T>(int pageNumber, int pageSize, Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default) where T : class => throw new NotImplementedException();
        public Task<PagedList<T>> GetPagedListAsync<T>(int pageNumber, int pageSize, Expression<Func<T, bool>>? predicate, string? sortBy, string? sortOrder = null, string? defaultSortBy = null, CancellationToken cancellationToken = default) where T : class => throw new NotImplementedException();
        public Task<PagedList<T>> GetPagedListAsync<T>(int pageNumber, int pageSize, Expression<Func<T, bool>>? predicate, Func<IQueryable<T>, IQueryable<T>>? sortFunc, CancellationToken cancellationToken = default) where T : class => throw new NotImplementedException();
    }

    private sealed class StubCurrentUserService : ICurrentUserService
    {
        public string? UserId => null;
        public string? UserName => null;
        public bool IsAuthenticated => false;
        public bool IsInRole(string role) => false;
        public string? IpAddress => null;
        public string? UserAgent => null;
    }
}
