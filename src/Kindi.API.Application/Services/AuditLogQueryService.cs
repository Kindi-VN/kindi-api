using AutoMapper;
using Kindi.API.Application.Common.Extensions;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.Common.Mappings;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Models;
using Kindi.API.Shared.Common.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Kindi.API.Application.Services;

public class AuditLogQueryService : IAuditLogQueryService
{
    private readonly IQueryService _queryService;
    private readonly IMapper _mapper;

    public AuditLogQueryService(
        IQueryService queryService,
        IMapper mapper)
    {
        _queryService = queryService;
        _mapper = mapper;
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
            .WhereIf(query.FromDate.HasValue, x => x.Timestamp >= query.FromDate!.Value.ToUniversalTime())
            .WhereIf(query.ToDate.HasValue, x => x.Timestamp <= query.ToDate!.Value.ToUniversalTime());

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
            .WhereIf(query.FromDate.HasValue, x => x.Timestamp >= query.FromDate!.Value.ToUniversalTime())
            .WhereIf(query.ToDate.HasValue, x => x.Timestamp <= query.ToDate!.Value.ToUniversalTime());

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