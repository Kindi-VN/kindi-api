using AutoMapper;
using Kindi.API.Application.Common.Extensions;
using Kindi.API.Application.Common.Helpers;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.Common.Mappings;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Domain.Models;
using Kindi.API.Shared.Common.Helpers;
using Kindi.API.Shared.Common.Interfaces;
using Kindi.API.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Kindi.API.Application.Services;

public class AuditLogQueryService : IAuditLogQueryService
{
    private readonly IQueryService _queryService;
    private readonly IMapper _mapper;
    private readonly IRepository<AuditLog> _auditLogRepository;
    private readonly IRepository<AuthAuditLog> _authAuditLogRepository;
    private readonly ICurrentUserService _currentUserService;

    public AuditLogQueryService(
        IQueryService queryService,
        IMapper mapper,
        IRepository<AuditLog> auditLogRepository,
        IRepository<AuthAuditLog> authAuditLogRepository,
        ICurrentUserService currentUserService)
    {
        _queryService = queryService;
        _mapper = mapper;
        _auditLogRepository = auditLogRepository;
        _authAuditLogRepository = authAuditLogRepository;
        _currentUserService = currentUserService;
    }

    public async Task<PagedList<AuditLogDto>> GetEntityLogsAsync(
        AuditLogQueryDto query,
        bool includeSuperAdminActors = false,
        CancellationToken cancellationToken = default)
    {
        var q = _queryService.GetQueryableNoTracking<AuditLog>()
            .WhereIf(!string.IsNullOrEmpty(query.EntityName), x => x.EntityName.Contains(query.EntityName!))
            .WhereIf(!string.IsNullOrEmpty(query.Action), x => x.Action == query.Action)
            .WhereIf(!string.IsNullOrEmpty(query.ActorId), x => x.ActorId == query.ActorId)
            .WhereIf(query.FromDate.HasValue, x => x.Timestamp >= DateRangeBounds.NormalizeFrom(query.FromDate))
            .WhereIf(query.ToDate.HasValue, x => x.Timestamp <= DateRangeBounds.NormalizeTo(query.ToDate));

        if (!includeSuperAdminActors)
        {
            q = ExcludeSuperAdminActor(q, await GetSuperAdminActorIdAsync(cancellationToken));
        }

        var result = await q.ToPagedListAsync(
            query.PageNumber, query.PageSize,
            query.SortBy, query.SortOrder,
            defaultSortBy: "Timestamp",
            cancellationToken);

        return _mapper.MapPagedList<AuditLog, AuditLogDto>(result);
    }

    public async Task<AuditLogDto?> GetEntityLogByIdAsync(Guid id, bool includeSuperAdminActors = false, CancellationToken cancellationToken = default)
    {
        var q = _queryService.GetQueryableNoTracking<AuditLog>();

        if (!includeSuperAdminActors)
        {
            q = ExcludeSuperAdminActor(q, await GetSuperAdminActorIdAsync(cancellationToken));
        }

        var entity = await q.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity == null ? null : _mapper.Map<AuditLogDto>(entity);
    }

    public async Task<PagedList<AuthAuditLogDto>> GetAuthLogsAsync(
        AuthAuditLogQueryDto query,
        bool includeSuperAdminActors = false,
        CancellationToken cancellationToken = default)
    {
        var q = _queryService.GetQueryableNoTracking<AuthAuditLog>()
            .WhereIf(!string.IsNullOrEmpty(query.Username), x => x.Username!.Contains(query.Username!))
            .WhereIf(!string.IsNullOrEmpty(query.Action), x => x.Action == query.Action)
            .WhereIf(!string.IsNullOrEmpty(query.OperatingSystem), x => x.OperatingSystem == query.OperatingSystem)
            .WhereIf(!string.IsNullOrEmpty(query.DeviceType), x => x.DeviceType == query.DeviceType)
            .WhereIf(query.IsSuccess.HasValue, x => x.IsSuccess == query.IsSuccess!.Value)
            .WhereIf(query.FromDate.HasValue, x => x.Timestamp >= DateRangeBounds.NormalizeFrom(query.FromDate))
            .WhereIf(query.ToDate.HasValue, x => x.Timestamp <= DateRangeBounds.NormalizeTo(query.ToDate));

        if (!includeSuperAdminActors)
        {
            q = ExcludeSuperAdminUser(q, await GetSuperAdminUserIdAsync(cancellationToken));
        }

        var result = await q.ToPagedListAsync(
            query.PageNumber, query.PageSize,
            query.SortBy, query.SortOrder,
            defaultSortBy: "Timestamp",
            cancellationToken);

        var paged = _mapper.MapPagedList<AuthAuditLog, AuthAuditLogDto>(result);
        foreach (var item in paged.Items)
            FillDeviceInfoIfMissing(item);

        return paged;
    }

    public async Task<AuthAuditLogDto?> GetAuthLogByIdAsync(Guid id, bool includeSuperAdminActors = false, CancellationToken cancellationToken = default)
    {
        var q = _queryService.GetQueryableNoTracking<AuthAuditLog>();

        if (!includeSuperAdminActors)
        {
            q = ExcludeSuperAdminUser(q, await GetSuperAdminUserIdAsync(cancellationToken));
        }

        var entity = await q.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity == null) return null;

        var dto = _mapper.Map<AuthAuditLogDto>(entity);
        FillDeviceInfoIfMissing(dto);
        return dto;
    }

    public async Task<AuditLogFilterOptionsResponse> GetFilterOptionsAsync(CancellationToken cancellationToken = default)
    {
        var entityNames = await _queryService.GetQueryableNoTracking<AuditLog>()
            .Select(x => x.EntityName)
            .Distinct()
            .ToListAsync(cancellationToken);

        return AuditLogFilterOptionsResponse.Build(entityNames);
    }

    public async Task<int> DeleteEntityLogsAsync(
        AuditLogDeleteRequest request,
        bool includeSuperAdminActors = false,
        CancellationToken cancellationToken = default)
    {
        var scope = AuditLogDeletionRules.Resolve(request.Ids, request.FromDate, request.ToDate);

        var query = _auditLogRepository.GetQueryable();
        if (!includeSuperAdminActors)
        {
            query = ExcludeSuperAdminActor(query, await GetSuperAdminActorIdAsync(cancellationToken));
        }

        var rows = await AuditLogDeletionRules.Apply(query, scope).ToListAsync(cancellationToken);
        if (rows.Count == 0)
            return 0;

        var deleted = await _auditLogRepository.HardDeleteRangeAsync(rows, cancellationToken);
        await WriteDeletionTraceAsync("AuditLog", scope, deleted, cancellationToken);

        return deleted;
    }

    public async Task<int> DeleteAuthLogsAsync(
        AuditLogDeleteRequest request,
        bool includeSuperAdminActors = false,
        CancellationToken cancellationToken = default)
    {
        var scope = AuditLogDeletionRules.Resolve(request.Ids, request.FromDate, request.ToDate);

        var query = _authAuditLogRepository.GetQueryable();
        if (!includeSuperAdminActors)
        {
            query = ExcludeSuperAdminUser(query, await GetSuperAdminUserIdAsync(cancellationToken));
        }

        var rows = await AuditLogDeletionRules.Apply(query, scope).ToListAsync(cancellationToken);
        if (rows.Count == 0)
            return 0;

        var deleted = await _authAuditLogRepository.HardDeleteRangeAsync(rows, cancellationToken);
        await WriteDeletionTraceAsync("AuthAuditLog", scope, deleted, cancellationToken);

        return deleted;
    }

    /// <summary>
    /// Ghi lại một dòng nhật ký cho chính thao tác xoá (bảng nhật ký không tự audit được) để luôn biết
    /// ai xoá, xoá theo điều kiện nào và mất bao nhiêu dòng.
    /// </summary>
    private async Task WriteDeletionTraceAsync(string entityName, AuditLogDeleteScope scope, int deletedCount,
        CancellationToken cancellationToken)
    {
        await _auditLogRepository.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = entityName,
            EntityId = Guid.Empty,
            Action = AuditAction.Delete,
            ActorId = _currentUserService.UserId,
            ActorName = _currentUserService.UserName,
            IpAddress = _currentUserService.IpAddress,
            DeviceType = null,
            Timestamp = DateTime.UtcNow,
            OldValues = JsonSerializer.Serialize(new
            {
                criteria = scope.IsBySelection ? "selection" : "dateRange",
                scope = AuditLogDeletionRules.Describe(scope),
                deletedCount
            })
        }, cancellationToken);
    }

    /// <summary>Id tài khoản SuperAdmin (tối đa 1 tài khoản) — để ẩn hành động của tài khoản này khỏi admin thường.</summary>
    private async Task<string?> GetSuperAdminActorIdAsync(CancellationToken cancellationToken)
        => (await GetSuperAdminUserIdAsync(cancellationToken))?.ToString();

    private async Task<Guid?> GetSuperAdminUserIdAsync(CancellationToken cancellationToken)
    {
        var id = await _queryService.GetAllNoTracking<User>()
            .IgnoreQueryFilters()
            .Where(u => u.Role == UserRole.SuperAdmin)
            .Select(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return id == Guid.Empty ? null : id;
    }

    private static IQueryable<AuditLog> ExcludeSuperAdminActor(IQueryable<AuditLog> query, string? actorId)
        => string.IsNullOrEmpty(actorId) ? query : query.Where(x => x.ActorId == null || x.ActorId != actorId);

    private static IQueryable<AuthAuditLog> ExcludeSuperAdminUser(IQueryable<AuthAuditLog> query, Guid? userId)
        => userId == null ? query : query.Where(x => x.UserId == null || x.UserId != userId);

    /// <summary>
    /// Với log ghi TRƯỚC khi thêm cột device info (OperatingSystem/BrowserName/DeviceType = null),
    /// parse fallback từ UserAgent tại thời điểm đọc.
    /// </summary>
    private static void FillDeviceInfoIfMissing(AuthAuditLogDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.OperatingSystem)
            && !string.IsNullOrWhiteSpace(dto.BrowserName)
            && !string.IsNullOrWhiteSpace(dto.DeviceType))
            return;

        var info = UserAgentParser.Parse(dto.UserAgent);
        dto.OperatingSystem ??= info.OperatingSystem;
        dto.BrowserName ??= info.BrowserName;
        dto.DeviceType ??= info.DeviceType;
    }
}