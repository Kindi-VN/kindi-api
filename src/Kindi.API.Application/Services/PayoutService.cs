namespace Kindi.API.Application.Services;

using Kindi.API.Application.Common.Exceptions;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Application.Resources;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Domain.Models;
using Kindi.API.Shared.Common.Interfaces;
using Kindi.API.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

/// <summary>
/// Giải ngân hoa hồng: hoa hồng đã đối soát được chi trả theo tháng (chốt sổ cuối tháng, chi trả đầu tháng sau);
/// thành viên có thể gửi yêu cầu rút sớm phần hoa hồng chưa tới kỳ và chịu phí rút sớm theo hạng/cấu hình chung.
/// </summary>
public sealed class PayoutService : IPayoutService
{
    private readonly IRepository<PayoutStatement> _statementRepository;
    private readonly IRepository<PayoutPeriod> _periodRepository;
    private readonly IRepository<PayoutSetting> _settingRepository;
    private readonly IRepository<UserBankAccount> _bankAccountRepository;
    private readonly IRepository<ReferralEvent> _referralEventRepository;
    private readonly IMembershipTierService _membershipTierService;
    private readonly ICommissionConfigService _commissionConfigService;
    private readonly IPermissionService _permissionService;
    private readonly IQueryService _queryService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ILogger<PayoutService> _logger;

    public PayoutService(
        IRepository<PayoutStatement> statementRepository,
        IRepository<PayoutPeriod> periodRepository,
        IRepository<PayoutSetting> settingRepository,
        IRepository<UserBankAccount> bankAccountRepository,
        IRepository<ReferralEvent> referralEventRepository,
        IMembershipTierService membershipTierService,
        ICommissionConfigService commissionConfigService,
        IPermissionService permissionService,
        IQueryService queryService,
        ICurrentUserService currentUserService,
        IStringLocalizer<SharedResource> localizer,
        ILogger<PayoutService> logger)
    {
        _statementRepository = statementRepository;
        _periodRepository = periodRepository;
        _settingRepository = settingRepository;
        _bankAccountRepository = bankAccountRepository;
        _referralEventRepository = referralEventRepository;
        _membershipTierService = membershipTierService;
        _commissionConfigService = commissionConfigService;
        _permissionService = permissionService;
        _queryService = queryService;
        _currentUserService = currentUserService;
        _localizer = localizer;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PayoutSettingResponse> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var setting = await _settingRepository.GetFirstAsync(x => !x.IsDeleted, cancellationToken);
        return setting == null
            ? new PayoutSettingResponse { IsEarlyWithdrawalEnabled = true }
            : MapSetting(setting);
    }

    /// <inheritdoc />
    public async Task<PayoutSettingResponse> SaveSettingsAsync(SavePayoutSettingRequest request, CancellationToken cancellationToken = default)
    {
        if (request.EarlyWithdrawalFeeRate is < 0 or > 100)
            throw new BusinessException(_localizer["Payout_FeeRateInvalid"]);

        if (request.MinWithdrawalAmount < 0)
            throw new BusinessException(_localizer["Payout_AmountInvalid"]);

        if (request.MinEarlyWithdrawalFee is < 0 || request.MaxEarlyWithdrawalFee is < 0)
            throw new BusinessException(_localizer["Payout_FeeAmountInvalid"]);

        if (request.MinEarlyWithdrawalFee.HasValue && request.MaxEarlyWithdrawalFee.HasValue
            && request.MinEarlyWithdrawalFee > request.MaxEarlyWithdrawalFee)
            throw new BusinessException(_localizer["Payout_FeeRangeInvalid"]);

        if (request.ClosingDay is < 0 or > 31)
            throw new BusinessException(_localizer["Payout_ClosingDayInvalid"]);

        var setting = await _settingRepository.GetQueryable()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => !x.IsDeleted, cancellationToken);

        var isNew = setting == null;
        setting ??= new PayoutSetting();

        setting.IsEarlyWithdrawalEnabled = request.IsEarlyWithdrawalEnabled;
        setting.EarlyWithdrawalFeeRate = request.EarlyWithdrawalFeeRate;
        setting.MinEarlyWithdrawalFee = request.MinEarlyWithdrawalFee;
        setting.MaxEarlyWithdrawalFee = request.MaxEarlyWithdrawalFee;
        setting.MinWithdrawalAmount = request.MinWithdrawalAmount;
        setting.ClosingDay = request.ClosingDay;
        setting.Note = request.Note?.Trim();
        setting.UpdatedAt = DateTime.UtcNow;
        setting.UpdatedBy = _currentUserService.UserName;

        if (isNew)
        {
            setting.Id = Guid.NewGuid();
            setting.CreatedAt = DateTime.UtcNow;
            setting.CreatedBy = _currentUserService.UserName;
            await _settingRepository.AddAsync(setting, cancellationToken);
        }
        else
        {
            _settingRepository.Update(setting);
        }

        await _settingRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Đã cập nhật cấu hình phí rút sớm: {Rate}%", setting.EarlyWithdrawalFeeRate);

        return MapSetting(setting);
    }

    /// <inheritdoc />
    public async Task<MyWalletResponse> GetWalletAsync(CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId();
        var settings = await GetSettingsAsync(cancellationToken);
        var membership = await _membershipTierService.GetMineAsync(cancellationToken);
        var bankAccount = await _bankAccountRepository.GetFirstAsync(x => x.UserId == userId, cancellationToken);

        // Hoa hồng đã đối soát nhưng chưa nằm trong kỳ chi trả nào là phần có thể rút sớm.
        var approvedCommission = await _queryService.GetAllNoTracking<ReferralEvent>()
            .Where(x => !x.IsDeleted && x.ReferrerUserId == userId && x.Status == ReferralEventStatus.Approved)
            .SumAsync(x => x.CommissionAmount ?? 0, cancellationToken);

        var statements = await _queryService.GetAllNoTracking<PayoutStatement>()
            .Where(x => !x.IsDeleted && x.UserId == userId)
            .Where(x => x.Status != PayoutStatus.Rejected && x.Status != PayoutStatus.Cancelled)
            .ToListAsync(cancellationToken);

        var accruedInStatements = statements.Sum(x => x.AccruedAmount);
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var withdrawnThisMonth = statements
            .Where(x => x.Type == PayoutType.Early && x.CreatedAt >= monthStart)
            .Sum(x => x.AccruedAmount);

        var limit = membership.Tier?.MonthlyWithdrawalLimit;
        var currentPeriod = await _periodRepository.GetFirstAsync(x => x.Status == PayoutPeriodStatus.Open, cancellationToken);

        return new MyWalletResponse
        {
            AvailableAmount = Math.Max(0, approvedCommission - accruedInStatements),
            PendingAmount = statements.Where(x => x.Status is PayoutStatus.Pending or PayoutStatus.Approved).Sum(x => x.NetAmount),
            PaidAmount = statements.Where(x => x.Status == PayoutStatus.Paid).Sum(x => x.NetAmount),
            WithdrawnThisMonth = withdrawnThisMonth,
            MonthlyLimit = limit,
            RemainingLimit = limit.HasValue ? Math.Max(0, limit.Value - withdrawnThisMonth) : null,
            EarlyWithdrawalFeeRate = membership.EffectiveEarlyWithdrawalFeeRate,
            MinEarlyWithdrawalFee = settings.MinEarlyWithdrawalFee,
            MaxEarlyWithdrawalFee = settings.MaxEarlyWithdrawalFee,
            MinWithdrawalAmount = settings.MinWithdrawalAmount,
            IsEarlyWithdrawalEnabled = settings.IsEarlyWithdrawalEnabled,
            HasBankAccount = bankAccount != null,
            BankAccountVerified = bankAccount?.IsVerified ?? false,
            Membership = membership,
            CurrentPeriod = currentPeriod == null ? null : await MapPeriodAsync(currentPeriod, cancellationToken),
            RecentPayouts = statements
                .OrderByDescending(x => x.CreatedAt)
                .Take(10)
                .Select(x => MapStatement(x, null, null))
                .ToList()
        };
    }

    /// <inheritdoc />
    public async Task<PagedList<PayoutStatementResponse>> GetPagedAsync(PayoutQueryDto query, CancellationToken cancellationToken = default)
    {
        var statements = _queryService.GetAllNoTracking<PayoutStatement>()
            .Include(x => x.User)
            .Include(x => x.PayoutPeriod)
            .Where(x => !x.IsDeleted);

        if (query.Type.HasValue)
            statements = statements.Where(x => x.Type == query.Type.Value);

        if (query.Status.HasValue)
            statements = statements.Where(x => x.Status == query.Status.Value);

        if (query.UserId.HasValue)
            statements = statements.Where(x => x.UserId == query.UserId.Value);

        if (query.PayoutPeriodId.HasValue)
            statements = statements.Where(x => x.PayoutPeriodId == query.PayoutPeriodId.Value);

        var keyword = query.Search?.Trim().ToLower();
        if (!string.IsNullOrEmpty(keyword))
            statements = statements.Where(x => x.User != null &&
                (x.User.Username.ToLower().Contains(keyword) || x.User.FullName.ToLower().Contains(keyword)));

        // Yêu cầu chờ duyệt lên trước để quản trị viên xử lý theo thứ tự ưu tiên.
        statements = statements
            .OrderBy(x => x.Status == PayoutStatus.Pending || x.Status == PayoutStatus.Approved ? 0 : 1)
            .ThenByDescending(x => x.RequestedAt ?? x.CreatedAt);

        var paged = await PagedList<PayoutStatement>.CreateAsync(statements, query.PageNumber, query.PageSize);
        return new PagedList<PayoutStatementResponse>(
            paged.Items.Select(x => MapStatement(x, x.User, x.PayoutPeriod)).ToList(),
            paged.TotalCount, paged.PageNumber, paged.PageSize);
    }

    /// <inheritdoc />
    public async Task<PayoutStatementResponse> CreateWithdrawalAsync(CreateWithdrawalRequest request, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId();
        var settings = await GetSettingsAsync(cancellationToken);

        if (!settings.IsEarlyWithdrawalEnabled)
            throw new BusinessException(_localizer["Payout_EarlyDisabled"]);

        if (request.Amount <= 0)
            throw new BusinessException(_localizer["Payout_AmountInvalid"]);

        if (request.Amount < settings.MinWithdrawalAmount)
            throw new BusinessException(_localizer["Payout_BelowMinAmount", settings.MinWithdrawalAmount.ToString("N0")]);

        var bankAccount = await _bankAccountRepository.GetFirstAsync(x => x.UserId == userId, cancellationToken)
            ?? throw new BusinessException(_localizer["Payout_NoBankAccount"]);

        // Chỉ cho rút khi tài khoản nhận tiền đã được xác minh, tránh giải ngân vào tài khoản chưa đối chiếu.
        if (!bankAccount.IsVerified)
            throw new BusinessException(_localizer["Payout_UnverifiedBankAccount"]);

        var wallet = await GetWalletAsync(cancellationToken);

        if (request.Amount > wallet.AvailableAmount)
            throw new BusinessException(_localizer["Payout_ExceedAvailable", wallet.AvailableAmount.ToString("N0")]);

        if (wallet.MonthlyLimit.HasValue && wallet.WithdrawnThisMonth + request.Amount > wallet.MonthlyLimit.Value)
            throw new BusinessException(_localizer["Payout_ExceedMonthlyLimit", wallet.MonthlyLimit.Value.ToString("N0")]);

        // Phí rút sớm: mức riêng của hạng trước, không có thì dùng mức chung; luôn kẹp trong khoảng min/max.
        var feeRate = wallet.Membership.Tier?.EarlyWithdrawalFeeRate ?? settings.EarlyWithdrawalFeeRate;
        var feeAmount = decimal.Round(request.Amount * feeRate / 100m, 0, MidpointRounding.AwayFromZero);

        if (settings.MinEarlyWithdrawalFee.HasValue && feeAmount < settings.MinEarlyWithdrawalFee.Value)
            feeAmount = settings.MinEarlyWithdrawalFee.Value;

        if (settings.MaxEarlyWithdrawalFee.HasValue && feeAmount > settings.MaxEarlyWithdrawalFee.Value)
            feeAmount = settings.MaxEarlyWithdrawalFee.Value;

        if (feeAmount > request.Amount)
            throw new BusinessException(_localizer["Payout_FeeExceedAmount"]);

        var period = await EnsurePeriodAsync(DateTime.UtcNow.Year, DateTime.UtcNow.Month, cancellationToken);

        var statement = new PayoutStatement
        {
            UserId = userId,
            PayoutPeriodId = period.Id,
            Type = PayoutType.Early,
            AccruedAmount = request.Amount,
            FeeRate = feeRate,
            FeeAmount = feeAmount,
            NetAmount = request.Amount - feeAmount,
            Status = PayoutStatus.Pending,
            BankName = bankAccount.BankName,
            BankBranch = bankAccount.Branch,
            BankAccountNumber = bankAccount.AccountNumber,
            BankAccountHolder = bankAccount.AccountHolder,
            RequestedAt = DateTime.UtcNow,
            CreatedBy = _currentUserService.UserName
        };

        await _statementRepository.AddAsync(statement, cancellationToken);
        await _statementRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Tài khoản {UserId} gửi yêu cầu rút sớm {Amount} (phí {Fee})", userId, request.Amount, feeAmount);

        return MapStatement(statement, null, period);
    }

    /// <inheritdoc />
    public Task<PayoutStatementResponse> ApproveAsync(Guid id, ProcessPayoutRequest request, CancellationToken cancellationToken = default)
        => ChangeStatusAsync(id, [PayoutStatus.Pending], PayoutStatus.Approved, request?.Note, cancellationToken);

    /// <inheritdoc />
    public Task<PayoutStatementResponse> RejectAsync(Guid id, ProcessPayoutRequest request, CancellationToken cancellationToken = default)
        => ChangeStatusAsync(id, [PayoutStatus.Pending, PayoutStatus.Approved], PayoutStatus.Rejected, request?.Note, cancellationToken);

    /// <inheritdoc />
    public Task<PayoutStatementResponse> MarkPaidAsync(Guid id, ProcessPayoutRequest request, CancellationToken cancellationToken = default)
        => ChangeStatusAsync(id, [PayoutStatus.Pending, PayoutStatus.Approved], PayoutStatus.Paid, request?.Note, cancellationToken);

    /// <inheritdoc />
    public async Task<PayoutStatementResponse> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId();
        var statement = await _statementRepository.GetFirstWithIncludesAsync(
            x => x.Id == id,
            query => query.Include(x => x.PayoutPeriod),
            cancellationToken) ?? throw new BusinessException(_localizer["Payout_StatementNotFound"]);

        if (statement.UserId != userId)
            throw new BusinessException(_localizer["Payout_NotOwner"]);

        if (statement.Type != PayoutType.Early || statement.Status != PayoutStatus.Pending)
            throw new BusinessException(_localizer["Payout_CancelInvalid"]);

        statement.Status = PayoutStatus.Cancelled;
        statement.ProcessedAt = DateTime.UtcNow;
        statement.ProcessedBy = _currentUserService.UserName;
        statement.UpdatedAt = DateTime.UtcNow;
        statement.UpdatedBy = _currentUserService.UserName;

        _statementRepository.Update(statement);
        await _statementRepository.SaveChangesAsync(cancellationToken);

        return MapStatement(statement, null, statement.PayoutPeriod);
    }

    /// <inheritdoc />
    public async Task<PagedList<PayoutPeriodResponse>> GetPeriodsAsync(PayoutPeriodQueryDto query, CancellationToken cancellationToken = default)
    {
        var periods = _queryService.GetAllNoTracking<PayoutPeriod>().Where(x => !x.IsDeleted);

        if (query.Year.HasValue)
            periods = periods.Where(x => x.Year == query.Year.Value);

        if (query.Status.HasValue)
            periods = periods.Where(x => x.Status == query.Status.Value);

        periods = periods.OrderByDescending(x => x.Year).ThenByDescending(x => x.Month);

        var paged = await PagedList<PayoutPeriod>.CreateAsync(periods, query.PageNumber, query.PageSize);
        var periodIds = paged.Items.Select(x => x.Id).ToList();

        var totals = await _queryService.GetAllNoTracking<PayoutStatement>()
            .Where(x => !x.IsDeleted && periodIds.Contains(x.PayoutPeriodId))
            .Where(x => x.Status != PayoutStatus.Rejected && x.Status != PayoutStatus.Cancelled)
            .GroupBy(x => x.PayoutPeriodId)
            .Select(g => new { PeriodId = g.Key, Count = g.Count(), Total = g.Sum(x => x.NetAmount) })
            .ToListAsync(cancellationToken);

        var items = paged.Items.Select(period =>
        {
            var total = totals.FirstOrDefault(x => x.PeriodId == period.Id);
            return MapPeriod(period, total?.Count ?? 0, total?.Total ?? 0);
        }).ToList();

        return new PagedList<PayoutPeriodResponse>(items, paged.TotalCount, paged.PageNumber, paged.PageSize);
    }

    /// <inheritdoc />
    public async Task<PayoutPeriodResponse> OpenPeriodAsync(CreatePayoutPeriodRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Month is < 1 or > 12)
            throw new BusinessException(_localizer["Payout_PeriodInvalid"]);

        if (request.Year is < 2020 or > 2100)
            throw new BusinessException(_localizer["Payout_PeriodInvalid"]);

        var period = await EnsurePeriodAsync(request.Year, request.Month, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            period.Note = request.Note.Trim();
            period.UpdatedAt = DateTime.UtcNow;
            period.UpdatedBy = _currentUserService.UserName;
            _periodRepository.Update(period);
            await _periodRepository.SaveChangesAsync(cancellationToken);
        }

        return await MapPeriodAsync(period, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PayoutPeriodResponse> ClosePeriodAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var period = await _periodRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new BusinessException(_localizer["Payout_PeriodNotFound"]);

        if (period.Status != PayoutPeriodStatus.Open)
            throw new BusinessException(_localizer["Payout_PeriodNotOpen"]);

        var periodStart = period.FromDate.Date;
        var periodEnd = period.ToDate.Date;

        var events = await _referralEventRepository.GetQueryable()
            .Where(x => !x.IsDeleted && x.ReferrerUserId != null)
            .Where(x => x.Status != ReferralEventStatus.Rejected && x.Status != ReferralEventStatus.Cancelled)
            .Where(x => x.CreatedAt >= periodStart && x.CreatedAt < periodEnd.AddDays(1))
            .ToListAsync(cancellationToken);

        var dirtyEvents = new List<ReferralEvent>();

        // Chỉ tính tiền cho người giới thiệu đã được bật chức năng hoa hồng (P013) và rút hoa hồng (P015):
        // tài khoản chưa bật thì phát sinh của họ không được ghi nhận hoa hồng nào.
        var commissionEnabled = await LoadCommissionEnabledAsync(
            events.Where(x => x.ReferrerUserId.HasValue).Select(x => x.ReferrerUserId!.Value), cancellationToken);

        // Sự kiện chưa có mức hoa hồng thì tính theo mức đang áp cho người giới thiệu rồi ghi lại để đối soát.
        foreach (var referralEvent in events.Where(x => x.CommissionAmount == null && x.ReferrerUserId.HasValue))
        {
            if (!commissionEnabled.Contains(referralEvent.ReferrerUserId!.Value))
                continue;

            var config = await _commissionConfigService.GetEffectiveAsync(
                CommissionBeneficiary.Referrer, referralEvent.ReferrerUserId!.Value, cancellationToken);

            var commission = ComputeCommission(referralEvent.Amount ?? 0, config);
            if (commission <= 0)
                continue;

            referralEvent.CommissionRate = config!.Type == CommissionType.Percentage ? config.Rate : null;
            referralEvent.CommissionAmount = commission;
            referralEvent.UpdatedAt = DateTime.UtcNow;
            referralEvent.UpdatedBy = _currentUserService.UserName;
            dirtyEvents.Add(referralEvent);
        }

        if (dirtyEvents.Count > 0)
            _referralEventRepository.UpdateRange(dirtyEvents);

        var byUser = events
            .GroupBy(x => x.ReferrerUserId!.Value)
            .Select(g => new { UserId = g.Key, Total = g.Sum(x => x.CommissionAmount ?? 0) })
            .Where(x => x.Total > 0)
            .ToList();

        var existing = await _statementRepository.GetQueryable()
            .IgnoreQueryFilters()
            .Where(x => x.PayoutPeriodId == period.Id && x.Type == PayoutType.Monthly)
            .ToListAsync(cancellationToken);

        var userIds = byUser.Select(x => x.UserId).ToList();
        var bankAccounts = await _queryService.GetAllNoTracking<UserBankAccount>()
            .Where(x => userIds.Contains(x.UserId))
            .ToListAsync(cancellationToken);

        var toAdd = new List<PayoutStatement>();
        var toUpdate = new List<PayoutStatement>();

        foreach (var total in byUser)
        {
            var bankAccount = bankAccounts.FirstOrDefault(x => x.UserId == total.UserId);
            var statement = existing.FirstOrDefault(x => x.UserId == total.UserId);

            if (statement == null)
            {
                toAdd.Add(new PayoutStatement
                {
                    UserId = total.UserId,
                    PayoutPeriodId = period.Id,
                    Type = PayoutType.Monthly,
                    AccruedAmount = total.Total,
                    FeeRate = 0,
                    FeeAmount = 0,
                    NetAmount = total.Total,
                    Status = PayoutStatus.Pending,
                    BankName = bankAccount?.BankName,
                    BankBranch = bankAccount?.Branch,
                    BankAccountNumber = bankAccount?.AccountNumber,
                    BankAccountHolder = bankAccount?.AccountHolder,
                    CreatedBy = _currentUserService.UserName
                });
            }
            else if (statement.Status != PayoutStatus.Paid)
            {
                statement.AccruedAmount = total.Total;
                statement.FeeAmount = 0;
                statement.NetAmount = total.Total;
                statement.BankName = bankAccount?.BankName;
                statement.BankBranch = bankAccount?.Branch;
                statement.BankAccountNumber = bankAccount?.AccountNumber;
                statement.BankAccountHolder = bankAccount?.AccountHolder;
                statement.IsDeleted = false;
                statement.UpdatedAt = DateTime.UtcNow;
                statement.UpdatedBy = _currentUserService.UserName;
                toUpdate.Add(statement);
            }
        }

        if (toAdd.Count > 0)
            await _statementRepository.AddRangeAsync(toAdd, cancellationToken);

        if (toUpdate.Count > 0)
            _statementRepository.UpdateRange(toUpdate);

        period.Status = PayoutPeriodStatus.Closed;
        period.ClosedAt = DateTime.UtcNow;
        period.PayoutDate = period.FromDate.Date.AddMonths(1);
        period.UpdatedAt = DateTime.UtcNow;
        period.UpdatedBy = _currentUserService.UserName;
        _periodRepository.Update(period);

        await _statementRepository.SaveChangesAsync(cancellationToken);
        await _periodRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Đã chốt sổ kỳ {Year}-{Month}: {Count} lần chi trả", period.Year, period.Month, byUser.Count);

        return await MapPeriodAsync(period, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PayoutPeriodResponse> PayPeriodAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var period = await _periodRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new BusinessException(_localizer["Payout_PeriodNotFound"]);

        if (period.Status != PayoutPeriodStatus.Closed)
            throw new BusinessException(_localizer["Payout_PeriodNotClosed"]);

        var statements = await _statementRepository.GetQueryable()
            .Where(x => !x.IsDeleted && x.PayoutPeriodId == period.Id)
            .Where(x => x.Status == PayoutStatus.Pending || x.Status == PayoutStatus.Approved)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var statement in statements)
        {
            statement.Status = PayoutStatus.Paid;
            statement.ProcessedAt = now;
            statement.ProcessedBy = _currentUserService.UserName;
            statement.UpdatedAt = now;
            statement.UpdatedBy = _currentUserService.UserName;
        }

        if (statements.Count > 0)
            _statementRepository.UpdateRange(statements);

        // Hoa hồng đã chi trả được đánh dấu để không tính lại ở các kỳ sau.
        var periodStart = period.FromDate.Date;
        var periodEnd = period.ToDate.Date;
        var events = await _referralEventRepository.GetQueryable()
            .Where(x => !x.IsDeleted && x.CreatedAt >= periodStart && x.CreatedAt < periodEnd.AddDays(1))
            .Where(x => x.Status == ReferralEventStatus.Approved || x.Status == ReferralEventStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var referralEvent in events)
        {
            referralEvent.Status = ReferralEventStatus.Paid;
            referralEvent.UpdatedAt = now;
            referralEvent.UpdatedBy = _currentUserService.UserName;
        }

        if (events.Count > 0)
            _referralEventRepository.UpdateRange(events);

        period.Status = PayoutPeriodStatus.Paid;
        period.PaidAt = now;
        period.UpdatedAt = now;
        period.UpdatedBy = _currentUserService.UserName;
        _periodRepository.Update(period);

        await _statementRepository.SaveChangesAsync(cancellationToken);
        await _referralEventRepository.SaveChangesAsync(cancellationToken);
        await _periodRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Đã chi trả kỳ {Year}-{Month}: {Count} lần chi trả", period.Year, period.Month, statements.Count);

        return await MapPeriodAsync(period, cancellationToken);
    }

    /// <summary>Đổi trạng thái một lần chi trả và ghi nhận người xử lý.</summary>
    private async Task<PayoutStatementResponse> ChangeStatusAsync(
        Guid id,
        PayoutStatus[] allowedStatuses,
        PayoutStatus status,
        string? note,
        CancellationToken cancellationToken)
    {
        var statement = await _statementRepository.GetFirstWithIncludesAsync(
            x => x.Id == id,
            query => query.Include(x => x.PayoutPeriod).Include(x => x.User),
            cancellationToken) ?? throw new BusinessException(_localizer["Payout_StatementNotFound"]);

        if (!allowedStatuses.Contains(statement.Status))
            throw new BusinessException(_localizer["Payout_StatusInvalid"]);

        if (status == PayoutStatus.Rejected && string.IsNullOrWhiteSpace(note))
            throw new BusinessException(_localizer["Payout_RejectReasonRequired"]);

        if (status == PayoutStatus.Paid && (statement.BankAccountNumber == null))
            throw new BusinessException(_localizer["Payout_NoBankAccount"]);

        statement.Status = status;
        statement.Note = note?.Trim();
        statement.ProcessedAt = DateTime.UtcNow;
        statement.ProcessedBy = _currentUserService.UserName;
        statement.UpdatedAt = DateTime.UtcNow;
        statement.UpdatedBy = _currentUserService.UserName;

        _statementRepository.Update(statement);
        await _statementRepository.SaveChangesAsync(cancellationToken);

        return MapStatement(statement, statement.User, statement.PayoutPeriod);
    }

    /// <summary>Lấy kỳ giải ngân của một tháng, chưa có thì mở mới (ngày chốt cuối tháng, chi trả đầu tháng sau).</summary>
    private async Task<PayoutPeriod> EnsurePeriodAsync(int year, int month, CancellationToken cancellationToken)
    {
        var period = await _periodRepository.GetFirstAsync(x => x.Year == year && x.Month == month, cancellationToken);
        if (period != null)
            return period;

        var fromDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        period = new PayoutPeriod
        {
            Year = year,
            Month = month,
            FromDate = fromDate,
            ToDate = fromDate.AddMonths(1).AddDays(-1),
            PayoutDate = fromDate.AddMonths(1),
            Status = PayoutPeriodStatus.Open,
            CreatedBy = _currentUserService.UserName
        };

        await _periodRepository.AddAsync(period, cancellationToken);
        await _periodRepository.SaveChangesAsync(cancellationToken);

        return period;
    }

    /// <summary>
    /// Người giới thiệu được tính hoa hồng khi tài khoản đã được bật chức năng hoa hồng (P013)
    /// và rút hoa hồng (P015) — quyền của vai trò hoặc cấu hình riêng của tài khoản đều tính.
    /// </summary>
    private async Task<HashSet<Guid>> LoadCommissionEnabledAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct().ToList();
        var enabled = new HashSet<Guid>();
        if (ids.Count == 0)
            return enabled;

        var users = await _queryService.GetListAsync<User>(u => ids.Contains(u.Id));
        var commissionCode = PermissionCode.ViewMyCommission.ToCode();
        var withdrawalCode = PermissionCode.RequestCommissionWithdrawal.ToCode();

        foreach (var user in users)
        {
            var permissions = await _permissionService.GetUserPermissionsAsync(user.Id, user.Role, cancellationToken);
            if (permissions.Codes.Contains(commissionCode) && permissions.Codes.Contains(withdrawalCode))
                enabled.Add(user.Id);
        }

        return enabled;
    }

    /// <summary>Tính hoa hồng của một phát sinh theo mức đang áp (%, số tiền cố định hoặc theo hạn mức).</summary>
    private static decimal ComputeCommission(decimal amount, CommissionConfigResponse? config)
    {
        if (config == null || !config.IsActive)
            return 0;

        if (config.MinOrderValue.HasValue && amount < config.MinOrderValue.Value)
            return 0;

        var commission = config.Type switch
        {
            CommissionType.Percentage => amount * config.Rate / 100m,
            CommissionType.Fixed => config.Rate,
            CommissionType.Tiered => FindTierRate(config, amount) is decimal rate ? amount * rate / 100m : 0m,
            _ => 0m
        };

        if (config.MaxCommission.HasValue && commission > config.MaxCommission.Value)
            commission = config.MaxCommission.Value;

        return decimal.Round(commission, 0, MidpointRounding.AwayFromZero);
    }

    /// <summary>Mức % của hạn mức khớp với giá trị phát sinh.</summary>
    private static decimal? FindTierRate(CommissionConfigResponse config, decimal amount)
        => config.Tiers
            .Where(x => x.FromValue <= amount && (!x.ToValue.HasValue || amount <= x.ToValue.Value))
            .OrderByDescending(x => x.FromValue)
            .Select(x => (decimal?)x.Rate)
            .FirstOrDefault();

    private Guid CurrentUserId()
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            throw new BusinessException(_localizer["Payout_UserInvalid"]);

        return userId;
    }

    private async Task<PayoutPeriodResponse> MapPeriodAsync(PayoutPeriod period, CancellationToken cancellationToken)
    {
        var totals = await _queryService.GetAllNoTracking<PayoutStatement>()
            .Where(x => !x.IsDeleted && x.PayoutPeriodId == period.Id)
            .Where(x => x.Status != PayoutStatus.Rejected && x.Status != PayoutStatus.Cancelled)
            .GroupBy(x => x.PayoutPeriodId)
            .Select(g => new { Count = g.Count(), Total = g.Sum(x => x.NetAmount) })
            .FirstOrDefaultAsync(cancellationToken);

        return MapPeriod(period, totals?.Count ?? 0, totals?.Total ?? 0);
    }

    private static PayoutPeriodResponse MapPeriod(PayoutPeriod period, int payoutCount, decimal totalNetAmount) => new()
    {
        Id = period.Id,
        Year = period.Year,
        Month = period.Month,
        PeriodLabel = $"{period.Month:00}/{period.Year}",
        FromDate = period.FromDate,
        ToDate = period.ToDate,
        Status = period.Status,
        ClosedAt = period.ClosedAt,
        PaidAt = period.PaidAt,
        PayoutDate = period.PayoutDate,
        Note = period.Note,
        PayoutCount = payoutCount,
        TotalNetAmount = totalNetAmount
    };

    private static PayoutSettingResponse MapSetting(PayoutSetting setting) => new()
    {
        IsEarlyWithdrawalEnabled = setting.IsEarlyWithdrawalEnabled,
        EarlyWithdrawalFeeRate = setting.EarlyWithdrawalFeeRate,
        MinEarlyWithdrawalFee = setting.MinEarlyWithdrawalFee,
        MaxEarlyWithdrawalFee = setting.MaxEarlyWithdrawalFee,
        MinWithdrawalAmount = setting.MinWithdrawalAmount,
        ClosingDay = setting.ClosingDay,
        Note = setting.Note
    };

    private static PayoutStatementResponse MapStatement(PayoutStatement statement, User? user, PayoutPeriod? period) => new()
    {
        Id = statement.Id,
        UserId = statement.UserId,
        Username = user?.Username,
        FullName = user?.FullName,
        UserCode = user?.UserCode,
        Type = statement.Type,
        AccruedAmount = statement.AccruedAmount,
        FeeRate = statement.FeeRate,
        FeeAmount = statement.FeeAmount,
        NetAmount = statement.NetAmount,
        Status = statement.Status,
        BankName = statement.BankName,
        BankBranch = statement.BankBranch,
        BankAccountNumber = statement.BankAccountNumber,
        BankAccountHolder = statement.BankAccountHolder,
        RequestedAt = statement.RequestedAt,
        ProcessedAt = statement.ProcessedAt,
        ProcessedBy = statement.ProcessedBy,
        Note = statement.Note,
        PayoutPeriodId = statement.PayoutPeriodId,
        PeriodLabel = period == null ? string.Empty : $"{period.Month:00}/{period.Year}",
        CreatedAt = statement.CreatedAt
    };
}
