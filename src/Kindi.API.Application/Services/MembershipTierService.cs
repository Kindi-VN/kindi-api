namespace Kindi.API.Application.Services;

using Kindi.API.Application.Common.Exceptions;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Application.Resources;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Shared.Common.Interfaces;
using Kindi.API.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

/// <summary>
/// Hạng thành viên: mỗi hạng có mức doanh số tối thiểu và các quyền lợi riêng (mức hoa hồng, phí rút sớm,
/// hạn mức rút trong tháng, thứ tự ưu tiên duyệt). Hạng của một tài khoản được xét theo doanh số/hoa hồng
/// tích luỹ từ hoạt động mua chung, offer và giới thiệu.
/// </summary>
public sealed class MembershipTierService : IMembershipTierService
{
    private readonly IRepository<MembershipTier> _tierRepository;
    private readonly IRepository<UserMembership> _membershipRepository;
    private readonly IQueryService _queryService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ILogger<MembershipTierService> _logger;

    public MembershipTierService(
        IRepository<MembershipTier> tierRepository,
        IRepository<UserMembership> membershipRepository,
        IQueryService queryService,
        ICurrentUserService currentUserService,
        IStringLocalizer<SharedResource> localizer,
        ILogger<MembershipTierService> logger)
    {
        _tierRepository = tierRepository;
        _membershipRepository = membershipRepository;
        _queryService = queryService;
        _currentUserService = currentUserService;
        _localizer = localizer;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<MembershipTierResponse>> GetTiersAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var query = _queryService.GetAllNoTracking<MembershipTier>().Where(x => !x.IsDeleted);
        if (activeOnly) query = query.Where(x => x.IsActive);

        var tiers = await query.OrderBy(x => x.Level).ToListAsync(cancellationToken);
        return tiers.Select(Map).ToList();
    }

    /// <inheritdoc />
    public async Task<MembershipTierResponse> SaveAsync(Guid? id, SaveMembershipTierRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new BusinessException(_localizer["Membership_NameRequired"]);

        if (request.MinAccumulatedValue < 0)
            throw new BusinessException(_localizer["Membership_AccumulatedInvalid"]);

        if (request.EarlyWithdrawalFeeRate is < 0 or > 100)
            throw new BusinessException(_localizer["Membership_FeeRateInvalid"]);

        if (request.MonthlyWithdrawalLimit is < 0)
            throw new BusinessException(_localizer["Membership_LimitInvalid"]);

        if (request.ApprovalPriority < 0)
            throw new BusinessException(_localizer["Membership_PriorityInvalid"]);

        // Thứ tự hạng là khoá tự nhiên: mỗi hạng chỉ có một bản ghi nên lưu theo hạng thay vì tạo trùng.
        var entity = id.HasValue
            ? await _tierRepository.GetByIdAsync(id.Value, cancellationToken)
            : await _tierRepository.GetFirstAsync(x => x.Level == request.Level, cancellationToken);

        if (id.HasValue && entity == null)
            throw new BusinessException(_localizer["Membership_TierNotFound"]);

        var isNew = entity == null;
        entity ??= new MembershipTier();

        entity.Name = request.Name.Trim();
        entity.Level = request.Level;
        entity.MinAccumulatedValue = request.MinAccumulatedValue;
        entity.EarlyWithdrawalFeeRate = request.EarlyWithdrawalFeeRate;
        entity.MonthlyWithdrawalLimit = request.MonthlyWithdrawalLimit;
        entity.ApprovalPriority = request.ApprovalPriority;
        entity.IsActive = request.IsActive;
        entity.Description = request.Description?.Trim();
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = _currentUserService.UserName;

        if (isNew)
        {
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            await _tierRepository.AddAsync(entity, cancellationToken);
        }
        else
        {
            _tierRepository.Update(entity);
        }

        await _tierRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Đã lưu hạng thành viên {Level} ({Name})", entity.Level, entity.Name);

        return Map(entity);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _tierRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new BusinessException(_localizer["Membership_TierNotFound"]);

        _tierRepository.Delete(entity);
        await _tierRepository.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<MembershipTierResponse>> GetDeletedAsync(CancellationToken cancellationToken = default)
    {
        // Bỏ global soft-delete filter để lấy các hạng đã xoá mềm.
        var tiers = await _tierRepository.GetQueryable()
            .IgnoreQueryFilters()
            .Where(x => x.IsDeleted)
            .OrderBy(x => x.Level)
            .ToListAsync(cancellationToken);

        return tiers.Select(Map).ToList();
    }

    /// <inheritdoc />
    public async Task<MembershipTierResponse> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // GetByIdIncludingDeletedAsync bỏ qua global filter → tìm được hạng đã xoá mềm.
        var entity = await _tierRepository.GetByIdIncludingDeletedAsync(id, cancellationToken)
            ?? throw new BusinessException(_localizer["Membership_TierNotFound"]);

        if (!entity.IsDeleted)
            throw new BusinessException(_localizer["Membership_TierNotFound"]);

        _tierRepository.Restore(entity);
        await _tierRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Đã khôi phục hạng thành viên {Level} ({Name})", entity.Level, entity.Name);

        return Map(entity);
    }

    /// <inheritdoc />
    public async Task<int> EvaluateAsync(EvaluateMembershipRequest request, CancellationToken cancellationToken = default)
    {
        var tiers = await _queryService.GetAllNoTracking<MembershipTier>()
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.MinAccumulatedValue)
            .ToListAsync(cancellationToken);

        if (tiers.Count == 0)
            return 0;

        // Doanh số tích luỹ: hoa hồng đã ghi nhận (hoặc giá trị phát sinh) của các sự kiện giới thiệu
        // không bị từ chối/huỷ — gồm mua chung, offer và người giới thiệu.
        var events = _queryService.GetAllNoTracking<ReferralEvent>()
            .Where(x => !x.IsDeleted && x.ReferrerUserId != null)
            .Where(x => x.Status != ReferralEventStatus.Rejected && x.Status != ReferralEventStatus.Cancelled);

        if (request.UserId.HasValue)
            events = events.Where(x => x.ReferrerUserId == request.UserId.Value);

        var totals = await events
            .GroupBy(x => x.ReferrerUserId!.Value)
            .Select(g => new { UserId = g.Key, Total = g.Sum(x => x.CommissionAmount ?? x.Amount ?? 0) })
            .ToListAsync(cancellationToken);

        if (totals.Count == 0)
            return 0;

        var userIds = totals.Select(x => x.UserId).ToList();
        var existing = await _membershipRepository.GetQueryable()
            .IgnoreQueryFilters()
            .Where(x => userIds.Contains(x.UserId))
            .ToListAsync(cancellationToken);

        var byUser = existing.ToDictionary(x => x.UserId);
        var now = DateTime.UtcNow;
        var toAdd = new List<UserMembership>();
        var toUpdate = new List<UserMembership>();

        foreach (var total in totals)
        {
            var tier = tiers.LastOrDefault(x => x.MinAccumulatedValue <= total.Total) ?? tiers.First();

            if (byUser.TryGetValue(total.UserId, out var membership))
            {
                membership.MembershipTierId = tier.Id;
                membership.AccumulatedValue = total.Total;
                membership.EvaluatedAt = now;
                membership.IsDeleted = false;
                membership.UpdatedAt = now;
                toUpdate.Add(membership);
            }
            else
            {
                toAdd.Add(new UserMembership
                {
                    UserId = total.UserId,
                    MembershipTierId = tier.Id,
                    AccumulatedValue = total.Total,
                    EvaluatedAt = now,
                    CreatedBy = _currentUserService.UserName
                });
            }
        }

        if (toAdd.Count > 0)
            await _membershipRepository.AddRangeAsync(toAdd, cancellationToken);

        if (toUpdate.Count > 0)
            _membershipRepository.UpdateRange(toUpdate);

        if (toAdd.Count > 0 || toUpdate.Count > 0)
            await _membershipRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Đã xét hạng thành viên cho {Count} tài khoản", totals.Count);
        return totals.Count;
    }

    /// <inheritdoc />
    public async Task<MyMembershipResponse> GetMineAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!Guid.TryParse(userId, out var parsedUserId))
            throw new BusinessException(_localizer["Payout_UserInvalid"]);

        var membership = await _membershipRepository.GetFirstWithIncludesAsync(
            x => x.UserId == parsedUserId,
            query => query.Include(x => x.MembershipTier),
            cancellationToken);

        // Xét lại khi chưa có hạng hoặc bản xét đã cũ để hạng hiển thị luôn đúng doanh số hiện tại.
        if (membership == null || membership.EvaluatedAt < DateTime.UtcNow.AddHours(-1))
        {
            await EvaluateAsync(new EvaluateMembershipRequest { UserId = parsedUserId }, cancellationToken);
            membership = await _membershipRepository.GetFirstWithIncludesAsync(
                x => x.UserId == parsedUserId,
                query => query.Include(x => x.MembershipTier),
                cancellationToken);
        }

        var tiers = await GetTiersAsync(true, cancellationToken);
        var accumulated = membership?.AccumulatedValue ?? 0;
        var settings = await _queryService.GetFirstOrDefaultAsync<PayoutSetting>(x => !x.IsDeleted, cancellationToken);

        var tier = membership == null ? null : tiers.FirstOrDefault(x => x.Id == membership.MembershipTierId);
        var nextTier = tiers.Where(x => x.MinAccumulatedValue > accumulated).OrderBy(x => x.MinAccumulatedValue).FirstOrDefault();

        return new MyMembershipResponse
        {
            Tier = tier,
            AccumulatedValue = accumulated,
            NextTier = nextTier,
            NextTierRequirement = nextTier == null ? null : nextTier.MinAccumulatedValue - accumulated,
            EvaluatedAt = membership?.EvaluatedAt,
            EffectiveEarlyWithdrawalFeeRate = tier?.EarlyWithdrawalFeeRate ?? settings?.EarlyWithdrawalFeeRate ?? 0,
            Tiers = tiers
        };
    }

    private static MembershipTierResponse Map(MembershipTier tier) => new()
    {
        Id = tier.Id,
        Level = tier.Level,
        Name = tier.Name,
        MinAccumulatedValue = tier.MinAccumulatedValue,
        EarlyWithdrawalFeeRate = tier.EarlyWithdrawalFeeRate,
        MonthlyWithdrawalLimit = tier.MonthlyWithdrawalLimit,
        ApprovalPriority = tier.ApprovalPriority,
        IsActive = tier.IsActive,
        Description = tier.Description
    };
}
