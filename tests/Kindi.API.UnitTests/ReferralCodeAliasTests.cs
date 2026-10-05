namespace Kindi.API.UnitTests;

using System.Linq.Expressions;
using System.Text.Json;
using AutoMapper;
using FluentAssertions;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.Common.Models;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.Requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Application.Features.OfferRequests.Commands;
using Kindi.API.Application.Features.PurchaseRequests.Commands;
using Kindi.API.Application.Mappings;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Domain.Models;
using Kindi.API.Shared.Common.Interfaces;
using Xunit;

/// <summary>
/// Chốt nhịp tương thích tên cũ của mã người giới thiệu: client cũ vẫn gửi "referralCode"
/// (hồ sơ CTV gửi "referredByCode") phải được đọc như tên chính recordReferrerCode / accountReferrerCode.
/// Quy tắc đã chọn: tên chính luôn được ưu tiên; tên cũ chỉ điền vào khi tên chính còn trống
/// (rỗng/thiếu), nên gửi cả hai giá trị khác nhau thì không ghi đè bừa giá trị tên chính.
/// </summary>
public class ReferralCodeAliasTests
{
    // Cấu hình JSON giống ASP.NET Core mặc định: camelCase + không phân biệt hoa/thường.
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    private static IMapper CreateMapper()
        => new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

    private static T Deserialize<T>(string json) where T : class
        => JsonSerializer.Deserialize<T>(json, WebJson)!;

    private static object Deserialize(Type dtoType, string json)
        => JsonSerializer.Deserialize(json, dtoType, WebJson)!;

    private static string? RecordReferrerCodeOf(object dto)
        => (string?)dto.GetType().GetProperty(nameof(CreateOfferRequestDto.RecordReferrerCode))!.GetValue(dto);

    private const string OnlyLegacy = """{"referralCode":"CTV-OLD"}""";
    private const string OnlyPrimary = """{"recordReferrerCode":"CTV-NEW"}""";
    private const string BothLegacyFirst = """{"referralCode":"CTV-OLD","recordReferrerCode":"CTV-NEW"}""";
    private const string BothPrimaryFirst = """{"recordReferrerCode":"CTV-NEW","referralCode":"CTV-OLD"}""";
    private const string PrimaryBlankLegacy = """{"recordReferrerCode":"","referralCode":"CTV-OLD"}""";

    [Theory]
    [InlineData(typeof(CreateOfferRequestDto))]
    [InlineData(typeof(CreatePurchaseRequestDto))]
    [InlineData(typeof(CreateGroupBuyingRequestDto))]
    [InlineData(typeof(JoinBusinessGroupRequest))]
    [InlineData(typeof(JoinGroupBuyingRequestDto))]
    [InlineData(typeof(PartnerRegisterRequest))]
    public void Chi_gui_ten_cu_referralCode_thi_van_nhan(Type dtoType)
    {
        RecordReferrerCodeOf(Deserialize(dtoType, OnlyLegacy)).Should().Be("CTV-OLD");
    }

    [Theory]
    [InlineData(typeof(CreateOfferRequestDto))]
    [InlineData(typeof(CreatePurchaseRequestDto))]
    [InlineData(typeof(CreateGroupBuyingRequestDto))]
    [InlineData(typeof(JoinBusinessGroupRequest))]
    [InlineData(typeof(JoinGroupBuyingRequestDto))]
    [InlineData(typeof(PartnerRegisterRequest))]
    public void Chi_gui_ten_chinh_recordReferrerCode_thi_van_nhan(Type dtoType)
    {
        RecordReferrerCodeOf(Deserialize(dtoType, OnlyPrimary)).Should().Be("CTV-NEW");
    }

    [Theory]
    [InlineData(typeof(CreateOfferRequestDto))]
    [InlineData(typeof(CreatePurchaseRequestDto))]
    [InlineData(typeof(CreateGroupBuyingRequestDto))]
    [InlineData(typeof(JoinBusinessGroupRequest))]
    [InlineData(typeof(JoinGroupBuyingRequestDto))]
    [InlineData(typeof(PartnerRegisterRequest))]
    public void Gui_ca_hai_thi_ten_chinh_duoc_uu_tien_du_thu_tu_khoa_json(Type dtoType)
    {
        RecordReferrerCodeOf(Deserialize(dtoType, BothLegacyFirst)).Should().Be("CTV-NEW");
        RecordReferrerCodeOf(Deserialize(dtoType, BothPrimaryFirst)).Should().Be("CTV-NEW");
    }

    [Theory]
    [InlineData(typeof(CreateOfferRequestDto))]
    [InlineData(typeof(CreatePurchaseRequestDto))]
    [InlineData(typeof(CreateGroupBuyingRequestDto))]
    [InlineData(typeof(JoinBusinessGroupRequest))]
    [InlineData(typeof(JoinGroupBuyingRequestDto))]
    [InlineData(typeof(PartnerRegisterRequest))]
    public void Ten_chinh_rong_thi_moi_lay_ten_cu(Type dtoType)
    {
        RecordReferrerCodeOf(Deserialize(dtoType, PrimaryBlankLegacy)).Should().Be("CTV-OLD");
    }

    [Fact]
    public void Ten_cu_chi_de_doc_va_khong_lo_vao_json_dau_ra()
    {
        var json = JsonSerializer.Serialize(new CreateOfferRequestDto { RecordReferrerCode = "CTV-1" }, WebJson);
        json.Should().NotContain("\"referralCode\"");
    }

    [Fact]
    public void Ho_so_ctv_nhan_ten_cu_referredByCode()
    {
        var dto = Deserialize<CreateCollaboratorDto>("""{"referredByCode":"CTV-OLD"}""");
        dto.AccountReferrerCode.Should().Be("CTV-OLD");
    }

    [Fact]
    public void Ten_cu_chay_qua_mapper_vao_command_tao_offer()
    {
        var dto = Deserialize<CreateOfferRequestDto>(OnlyLegacy);
        var command = CreateMapper().Map<CreateOfferRequestCommand>(dto);
        command.RecordReferrerCode.Should().Be("CTV-OLD");
    }

    [Fact]
    public void Ten_cu_chay_qua_mapper_vao_command_tao_purchase()
    {
        var dto = Deserialize<CreatePurchaseRequestDto>(OnlyLegacy);
        var command = CreateMapper().Map<CreatePurchaseRequestCommand>(dto);
        command.RecordReferrerCode.Should().Be("CTV-OLD");
    }

    [Fact]
    public async Task Chi_gui_referralCode_thi_ban_ghi_ghi_nhan_ma_va_phat_sinh_event()
    {
        // Client cũ: chỉ có "referralCode" → DTO → (AutoMapper) → Command → Handler.
        var dto = Deserialize<CreateOfferRequestDto>(
            """{"referralCode":"CTV-OLD","productName":"Sữa","unit":"hộp","fullName":"Khách","phone":"0901234567","currentPrice":100000}""");
        var command = CreateMapper().Map<CreateOfferRequestCommand>(dto);

        var repository = new RecordingRepository<OfferRequest>();
        var referralService = new RecordingReferralService();
        var handler = new CreateOfferRequestCommandHandler(
            repository, CreateMapper(), new StubCurrentUserService(Guid.NewGuid()),
            new StubUserService(), referralService);

        await handler.Handle(command, CancellationToken.None);

        // Mã tên cũ đi đúng vào lời gọi ResolveForUserAsync của luồng cũ.
        referralService.ResolveInputs.Should().ContainSingle().Which.Should().Be("CTV-OLD");
        // Bản ghi lưu mã đã chuẩn hoá và event ghi công được phát sinh.
        var saved = repository.Added.Should().ContainSingle().Subject;
        saved.RecordReferrerCode.Should().Be("CTV-RESOLVED");
        referralService.Events.Should().ContainSingle()
            .Which.ReferralCode.Should().Be("CTV-RESOLVED");
        referralService.Events.Single().Type.Should().Be(ReferralEventType.OfferRequest);
    }

    [Fact]
    public async Task Gui_ca_hai_khac_nhau_thi_handler_dung_gia_tri_ten_chinh()
    {
        var dto = Deserialize<CreateOfferRequestDto>(
            """{"referralCode":"CTV-OLD","recordReferrerCode":"CTV-NEW","productName":"Sữa","unit":"hộp","fullName":"Khách","phone":"0901234567","currentPrice":100000}""");
        var command = CreateMapper().Map<CreateOfferRequestCommand>(dto);

        var repository = new RecordingRepository<OfferRequest>();
        var referralService = new RecordingReferralService();
        var handler = new CreateOfferRequestCommandHandler(
            repository, CreateMapper(), new StubCurrentUserService(Guid.NewGuid()),
            new StubUserService(), referralService);

        await handler.Handle(command, CancellationToken.None);

        // Không ghi đè bừa: giá trị tên chính được dùng, giá trị tên cũ bị bỏ qua.
        referralService.ResolveInputs.Should().ContainSingle().Which.Should().Be("CTV-NEW");
        repository.Added.Should().ContainSingle().Subject.RecordReferrerCode.Should().Be("CTV-RESOLVED");
        referralService.Events.Should().HaveCount(1);
    }

    private sealed class StubCurrentUserService : ICurrentUserService
    {
        public StubCurrentUserService(Guid userId) => UserId = userId.ToString();

        public string? UserId { get; }
        public string? UserName => null;
        public bool IsAuthenticated => true;
        public bool IsInRole(string role) => false;
        public string? IpAddress => null;
        public string? UserAgent => null;
    }

    private sealed class StubUserService : IUserService
    {
        public Task<User?> GetCurrentUserAsync() => Task.FromResult<User?>(null!);
        public Task<Guid> GetOrCreateUserAsync(string fullName, string phone, string? email = null) => Task.FromResult(Guid.NewGuid());
        public Task<User?> FindByPhoneOrEmailAsync(string? phone, string? email) => Task.FromResult<User?>(null);
        public Task<UserPersonalInfo?> GetPersonalInfoAsync(Guid userId) => Task.FromResult<UserPersonalInfo?>(null);
        public Task UpdatePersonalInfoAsync(Guid userId, string? fullName, string? phone, string? email, string? zalo, bool allowContactChange = false)
            => Task.CompletedTask;

        public Task<User?> FindByIdAsync(Guid userId) => throw new NotImplementedException();
        public Task<PublicUserResult> ResolvePublicUserAsync(string fullName, string phone, string? email, string? zalo) => throw new NotImplementedException();
        public Task<PagedList<UserInfoResponse>> GetPagedAsync(UserQueryDto query) => throw new NotImplementedException();
        public Task<UserInfoResponse?> ResetPasswordToPhoneAsync(Guid userId) => throw new NotImplementedException();
    }

    private sealed class RecordingReferralService : IReferralService
    {
        public List<string?> ResolveInputs { get; } = new();
        public List<(string? ReferralCode, ReferralEventType Type)> Events { get; } = new();

        public Task<string?> ResolveForUserAsync(Guid userId, string? referralCode)
        {
            ResolveInputs.Add(referralCode);
            return Task.FromResult(string.IsNullOrWhiteSpace(referralCode) ? null : "CTV-RESOLVED");
        }

        public Task RecordEventAsync(string? referralCode, Guid referredUserId, ReferralEventType eventType,
            Guid? refEntityId = null, string? refEntityCode = null, decimal? amount = null, bool isGuestAccount = false)
        {
            Events.Add((referralCode, eventType));
            return Task.CompletedTask;
        }

        public Task<string?> GetSharerReferralCodeAsync() => throw new NotImplementedException();
        public Task<string?> ResolveAsync(string? referralCode) => throw new NotImplementedException();
        public Task<string?> AttributeToCurrentUserAsync(string? referralCode) => throw new NotImplementedException();
        public Task SetEventStatusAsync(ReferralEventType eventType, Guid refEntityId, ReferralEventStatus? status) => throw new NotImplementedException();
        public Task SetEventStatusAsync(ReferralEventType eventType, IEnumerable<Guid> refEntityIds, ReferralEventStatus? status) => throw new NotImplementedException();
        public Task<Dictionary<string, string>> LoadNamesAsync(IEnumerable<string?> referralCodes) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<string>> FindReferrerCodesByNameAsync(string searchTerm, bool unaccentAndCaseInsensitive) => throw new NotImplementedException();
        public Task FillNamesAsync<T>(IEnumerable<T> items, Func<T, string?> getReferralCode, Action<T, string> setReferralName) => throw new NotImplementedException();
    }

    private sealed class RecordingRepository<T> : IRepository<T> where T : class
    {
        public List<T> Added { get; } = new();

        public Task AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            Added.Add(entity);
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

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
        public void Update(T entity) => throw new NotImplementedException();
        public void UpdateRange(IEnumerable<T> entities) => throw new NotImplementedException();
        public void Delete(T entity) => throw new NotImplementedException();
        public void DeleteRange(IEnumerable<T> entities) => throw new NotImplementedException();
        public void Restore(T entity) => throw new NotImplementedException();
        public void RestoreRange(IEnumerable<T> entities) => throw new NotImplementedException();
    }
}
