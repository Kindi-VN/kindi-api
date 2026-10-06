namespace Kindi.API.Application.Services;

using Kindi.API.Application.Common.Exceptions;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Shared.Common.Interfaces;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Application.Resources;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Domain.Models;
using Kindi.API.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

/// <summary>
/// Cấu hình hoa hồng theo bên nhận (người giới thiệu / đối tác): mỗi bên có một bản chung và các bản riêng
/// cho từng tài khoản. Bản riêng luôn được ưu tiên hơn bản chung khi xác định mức áp dụng.
/// </summary>
public sealed class CommissionConfigService : ICommissionConfigService
{
    private readonly IRepository<CommissionConfig> _configRepository;
    private readonly IRepository<CommissionTier> _tierRepository;
    private readonly IQueryService _queryService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ILogger<CommissionConfigService> _logger;

    public CommissionConfigService(
        IRepository<CommissionConfig> configRepository,
        IRepository<CommissionTier> tierRepository,
        IQueryService queryService,
        ICurrentUserService currentUserService,
        IStringLocalizer<SharedResource> localizer,
        ILogger<CommissionConfigService> logger)
    {
        _configRepository = configRepository;
        _tierRepository = tierRepository;
        _queryService = queryService;
        _currentUserService = currentUserService;
        _localizer = localizer;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PagedList<CommissionConfigResponse>> GetPagedAsync(CommissionConfigQueryDto query, CancellationToken cancellationToken = default)
    {
        // Tab "Đã xoá": bỏ global soft-delete filter để lấy các cấu hình đã xoá mềm.
        var configs = query.IsDeleted == true
            ? _queryService.GetQueryable<CommissionConfig>().IgnoreQueryFilters().AsNoTracking().Where(x => x.IsDeleted)
            : _queryService.GetAllNoTracking<CommissionConfig>();

        if (query.Beneficiary.HasValue)
            configs = configs.Where(x => x.Beneficiary == query.Beneficiary.Value);

        var keyword = query.Search?.Trim().ToLower();
        if (!string.IsNullOrEmpty(keyword))
            configs = configs.Where(x => x.User != null &&
                (x.User.FullName.Like(keyword) || x.User.Username.Like(keyword)));

        // Bản chung trước, rồi tới bản riêng mới cập nhật.
        configs = configs
            .OrderBy(x => x.UserId == null ? 0 : 1)
            .ThenByDescending(x => x.UpdatedAt ?? x.CreatedAt);

        var paged = await PagedList<CommissionConfig>.CreateAsync(
            configs.Include(x => x.User).Include(x => x.Tiers), query.PageNumber, query.PageSize);

        return new PagedList<CommissionConfigResponse>(
            paged.Items.Select(Map).ToList(), paged.TotalCount, paged.PageNumber, paged.PageSize);
    }

    /// <inheritdoc />
    public async Task<List<CommissionConfigResponse>> SaveAsync(SaveCommissionConfigRequest request, CancellationToken cancellationToken = default)
    {
        var userIds = request.UserIds.Distinct().ToList();
        if (!request.IsGlobal && userIds.Count == 0)
            throw new BusinessException(_localizer["Commission_NoTarget"]);

        if (userIds.Count > 0)
        {
            var foundUsers = await _queryService.GetAllNoTracking<User>()
                .Where(x => userIds.Contains(x.Id) && !x.IsDeleted)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            if (foundUsers.Count != userIds.Count)
                throw new BusinessException(_localizer["Commission_UserNotFound"]);
        }

        var current = await _configRepository.GetQueryable()
            .IgnoreQueryFilters()
            .Include(x => x.Tiers)
            .Where(x => x.Beneficiary == request.Beneficiary)
            .Where(x => x.UserId == null || userIds.Contains(x.UserId.Value))
            .ToListAsync(cancellationToken);

        var currentGlobal = current.FirstOrDefault(x => x.UserId == null && x.Beneficiary == request.Beneficiary);
        var currentByUser = current.Where(x => x.UserId.HasValue).GroupBy(x => x.UserId!.Value).ToDictionary(x => x.Key, x => x.First());

        var targets = new List<Guid?>();
        if (request.IsGlobal) targets.Add(null);
        targets.AddRange(userIds.Select(id => (Guid?)id));

        var toAdd = new List<CommissionConfig>();
        var toUpdate = new List<CommissionConfig>();
        var retiredTiers = new List<CommissionTier>();
        var addedTiers = new List<CommissionTier>();
        var saved = new List<CommissionConfig>();

        foreach (var targetId in targets)
        {
            CommissionConfig config;
            var isNew = false;

            if (targetId == null)
            {
                if (currentGlobal == null) { config = new CommissionConfig { Beneficiary = request.Beneficiary }; isNew = true; }
                else config = currentGlobal;
            }
            else if (currentByUser.TryGetValue(targetId.Value, out var existing))
            {
                config = existing;
            }
            else
            {
                config = new CommissionConfig { Beneficiary = request.Beneficiary, UserId = targetId.Value };
                isNew = true;
            }

            config.Type = request.Type;
            config.Rate = request.Rate;
            config.MinOrderValue = request.MinOrderValue;
            config.MaxCommission = request.MaxCommission;
            config.IsActive = request.IsActive;
            config.Note = request.Note;
            config.IsDeleted = false;

            foreach (var tier in config.Tiers.Where(x => !x.IsDeleted))
            {
                tier.IsDeleted = true;
                retiredTiers.Add(tier);
            }

            foreach (var tier in request.Type == CommissionType.Tiered ? request.Tiers : new List<CommissionTierRequest>())
            {
                addedTiers.Add(new CommissionTier
                {
                    CommissionConfigId = config.Id,
                    FromValue = tier.FromValue,
                    ToValue = tier.ToValue,
                    Rate = tier.Rate
                });
            }

            if (isNew) toAdd.Add(config); else toUpdate.Add(config);
            saved.Add(config);
        }

        if (toUpdate.Count > 0) _configRepository.UpdateRange(toUpdate);
        if (retiredTiers.Count > 0) _tierRepository.UpdateRange(retiredTiers);
        if (toUpdate.Count > 0 || retiredTiers.Count > 0)
            await _configRepository.SaveChangesAsync(cancellationToken);

        if (toAdd.Count > 0) await _configRepository.AddRangeAsync(toAdd, cancellationToken);
        if (addedTiers.Count > 0) await _tierRepository.AddRangeAsync(addedTiers, cancellationToken);

        _logger.LogInformation("Đã lưu cấu hình hoa hồng {Beneficiary} cho {Count} phạm vi", request.Beneficiary, targets.Count);

        return saved.Select(Map).ToList();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var config = await _configRepository.GetQueryable()
            .Include(x => x.Tiers)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(_localizer["Commission_NotFound"]);

        config.IsDeleted = true;
        var tiers = config.Tiers.Where(x => !x.IsDeleted).ToList();
        if (tiers.Count > 0)
        {
            foreach (var tier in tiers) tier.IsDeleted = true;
            _tierRepository.UpdateRange(tiers);
        }

        _configRepository.Update(config);
        await _configRepository.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CommissionConfigResponse> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Bỏ global filter để tìm cấu hình đã xoá (kèm các bậc bị xoá cùng) rồi khôi phục cả cụm.
        var config = await _configRepository.GetQueryable()
            .IgnoreQueryFilters()
            .Include(x => x.Tiers)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (config == null || !config.IsDeleted)
            throw new NotFoundException(_localizer["Commission_NotFound"]);

        config.IsDeleted = false;
        foreach (var tier in config.Tiers.Where(x => x.IsDeleted))
            tier.IsDeleted = false;

        _configRepository.Update(config);
        await _configRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Đã khôi phục cấu hình hoa hồng {Beneficiary}", config.Beneficiary);

        return Map(config);
    }

    /// <inheritdoc />
    public async Task<MyCommissionResponse> GetMineAsync(CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            throw new UnauthorizedException(_localizer["Commission_Unauthorized"]);

        return new MyCommissionResponse
        {
            Referrer = await GetEffectiveAsync(CommissionBeneficiary.Referrer, userId, cancellationToken),
            Partner = await GetEffectiveAsync(CommissionBeneficiary.Partner, userId, cancellationToken)
        };
    }

    /// <inheritdoc />
    public async Task<List<CommissionUserResponse>> SearchUsersAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = _queryService.GetAllNoTracking<User>()
            .Where(x => !x.IsDeleted && x.Role != UserRole.SuperAdmin);

        var keyword = search?.Trim().ToLower();
        if (!string.IsNullOrEmpty(keyword))
            query = query.Where(x => x.Username.Like(keyword)
                || x.FullName.Like(keyword)
                || (x.Phone != null && x.Phone.Like(keyword)));

        return await query
            .OrderBy(x => x.Username)
            .Take(50)
            .Select(x => new CommissionUserResponse
            {
                Id = x.Id,
                Username = x.Username,
                FullName = x.FullName,
                Role = ((int)x.Role).ToString()
            })
            .ToListAsync(cancellationToken);
    }

    /// <summary>Mức đang áp cho một tài khoản: bản riêng của tài khoản trước, không có thì lấy bản chung.</summary>
    public async Task<CommissionConfigResponse?> GetEffectiveAsync(CommissionBeneficiary beneficiary, Guid userId, CancellationToken cancellationToken)
    {
        var config = await _queryService.GetAllNoTracking<CommissionConfig>()
            .Where(x => !x.IsDeleted && x.IsActive && x.Beneficiary == beneficiary &&
                        (x.UserId == null || x.UserId == userId))
            .Include(x => x.User)
            .Include(x => x.Tiers)
            .OrderBy(x => x.UserId == null ? 1 : 0)
            .ThenByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return config == null ? null : Map(config);
    }

    private static CommissionConfigResponse Map(CommissionConfig config) => new()
    {
        Id = config.Id,
        Beneficiary = config.Beneficiary,
        UserId = config.UserId,
        UserFullName = config.User?.FullName,
        Username = config.User?.Username,
        Type = config.Type,
        Rate = config.Rate,
        MinOrderValue = config.MinOrderValue,
        MaxCommission = config.MaxCommission,
        IsActive = config.IsActive,
        Note = config.Note,
        UpdatedAt = config.UpdatedAt ?? config.CreatedAt,
        Tiers = config.Tiers.Where(x => !x.IsDeleted)
            .OrderBy(x => x.FromValue)
            .Select(x => new CommissionTierResponse { Id = x.Id, FromValue = x.FromValue, ToValue = x.ToValue, Rate = x.Rate })
            .ToList()
    };
}
