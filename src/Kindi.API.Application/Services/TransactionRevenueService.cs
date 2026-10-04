namespace Kindi.API.Application.Services;

using System.Globalization;
using Kindi.API.Application.Common.Exceptions;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Application.Resources;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Domain.Models;
using Kindi.API.Domain.Rules;
using Kindi.API.Shared.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

/// <summary>
/// Khai doanh thu giao dịch: quản trị viên nhập doanh thu gộp, tỷ lệ thuế lấy mặc định từ cài đặt chung
/// (ghi đè được cho từng giao dịch), hoa hồng từng bên do quản trị viên nhập theo thoả thuận thật.
///
/// Số liệu được tính lại ở mỗi lần lưu và một lần nữa lúc chốt; tỷ lệ dùng lúc chốt lưu ngay trên bản ghi
/// nên sau này đổi cấu hình không làm lệch số cũ.
/// </summary>
public sealed class TransactionRevenueService : ITransactionRevenueService
{
    private readonly IRepository<TransactionRevenue> _revenueRepository;
    private readonly IRepository<TransactionCommission> _commissionRepository;
    private readonly IRepository<SystemSetting> _settingRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ILogger<TransactionRevenueService> _logger;

    public TransactionRevenueService(
        IRepository<TransactionRevenue> revenueRepository,
        IRepository<TransactionCommission> commissionRepository,
        IRepository<SystemSetting> settingRepository,
        ICurrentUserService currentUserService,
        IStringLocalizer<SharedResource> localizer,
        ILogger<TransactionRevenueService> logger)
    {
        _revenueRepository = revenueRepository;
        _commissionRepository = commissionRepository;
        _settingRepository = settingRepository;
        _currentUserService = currentUserService;
        _localizer = localizer;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PagedList<TransactionRevenueResponse>> GetPagedAsync(RevenueQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = _revenueRepository.GetQueryable().Include(r => r.Commissions).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            source = source.Where(r => r.ReferenceCode.Contains(keyword));
        }

        if (query.Type.HasValue)
            source = source.Where(r => r.Type == query.Type.Value);

        if (query.Status.HasValue)
            source = source.Where(r => r.Status == query.Status.Value);

        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > 100 ? 10 : query.PageSize;
        var paged = await PagedList<TransactionRevenue>.CreateAsync(source.OrderByDescending(r => r.CreatedAt), pageNumber, pageSize);

        return new PagedList<TransactionRevenueResponse>(
            paged.Items.Select(Map).ToList(), paged.TotalCount, paged.PageNumber, paged.PageSize);
    }

    /// <inheritdoc />
    public async Task<TransactionRevenueResponse?> GetByReferenceAsync(TransactionType type, Guid referenceId, CancellationToken cancellationToken = default)
    {
        var entity = await _revenueRepository.GetFirstWithIncludesAsync(
            r => r.Type == type && r.ReferenceId == referenceId,
            q => q.Include(r => r.Commissions),
            cancellationToken);

        return entity is null ? null : Map(entity);
    }

    /// <inheritdoc />
    public async Task<TransactionRevenueResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _revenueRepository.GetFirstWithIncludesAsync(
            r => r.Id == id,
            q => q.Include(r => r.Commissions),
            cancellationToken);

        return entity is null ? null : Map(entity);
    }

    /// <inheritdoc />
    public async Task<TransactionRevenueResponse> SaveAsync(SaveTransactionRevenueRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ReferenceId == Guid.Empty)
            throw new BusinessException(_localizer["Revenue_ReferenceRequired"]);

        var settings = await _settingRepository.GetFirstAsync(_ => true, cancellationToken);
        var taxPercent = request.TaxPercent ?? settings?.RevenueTaxPercent ?? 0m;
        var taxIncluded = request.TaxIncluded ?? settings?.RevenueTaxIncluded ?? false;

        Validate(request, taxPercent);
        var breakdown = Calculate(request, taxIncluded, taxPercent);

        var entity = await _revenueRepository.GetFirstWithIncludesAsync(
            r => r.Type == request.Type && r.ReferenceId == request.ReferenceId,
            q => q.Include(r => r.Commissions),
            cancellationToken);

        if (entity is null)
        {
            entity = new TransactionRevenue { Type = request.Type, ReferenceId = request.ReferenceId };
            Apply(entity, request, taxIncluded, taxPercent, breakdown);
            await _revenueRepository.AddAsync(entity, cancellationToken);
        }
        else
        {
            if (entity.Status == RevenueRecordStatus.Confirmed)
                throw new BusinessException(_localizer["Revenue_AlreadyConfirmed"]);

            Apply(entity, request, taxIncluded, taxPercent, breakdown);
            _revenueRepository.Update(entity);
        }

        await _revenueRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Đã lưu bản khai doanh thu giao dịch {Code}", entity.ReferenceCode);

        return Map(entity);
    }

    /// <inheritdoc />
    public async Task<TransactionRevenueResponse> ConfirmAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _revenueRepository.GetFirstWithIncludesAsync(
            r => r.Id == id,
            q => q.Include(r => r.Commissions),
            cancellationToken);

        if (entity is null)
            throw new BusinessException(_localizer["Revenue_NotFound"]);

        if (entity.Status == RevenueRecordStatus.Confirmed)
            throw new BusinessException(_localizer["Revenue_AlreadyConfirmed"]);

        // Tính lại lần cuối trước khi khoá sổ để số chốt khớp đúng tỷ lệ đang hiển thị.
        var breakdown = RevenueCalculator.Calculate(
            entity.GrossRevenue,
            entity.TaxIncluded,
            entity.TaxRate,
            entity.Commissions.Where(c => !c.IsDeleted)
                .Select(c => new RevenueCalculator.CommissionRate(c.Beneficiary, c.Rate)),
            entity.ExtraCost);

        entity.TaxAmount = breakdown.TaxAmount;
        entity.NetRevenue = breakdown.NetRevenue;
        entity.TotalCommission = breakdown.TotalCommission;
        entity.ActualRevenue = breakdown.ActualRevenue;

        foreach (var line in breakdown.Commissions)
        {
            var item = entity.Commissions.FirstOrDefault(c => !c.IsDeleted && c.Beneficiary == line.Beneficiary);
            if (item is not null)
            {
                item.Rate = line.RatePercent;
                item.Amount = line.Amount;
            }
        }

        entity.Status = RevenueRecordStatus.Confirmed;
        entity.ConfirmedBy = _currentUserService.UserName ?? _currentUserService.UserId;
        entity.ConfirmedAt = DateTime.UtcNow;
        _revenueRepository.Update(entity);

        await _revenueRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Đã chốt doanh thu giao dịch {Code} ({Id})", entity.ReferenceCode, entity.Id);

        return Map(entity);
    }

    /// <inheritdoc />
    public async Task<RevenueStatsResponse> GetStatsAsync(RevenueStatsQueryDto query, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        // Ngày người dùng gửi lên không kèm múi giờ; phải quy về UTC mới so được với mốc thời gian
        // trong bảng (cột lưu dạng timestamp kèm múi giờ).
        var from = query.From?.Date.ToUniversalTime() ?? new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = query.To?.Date.ToUniversalTime() ?? today;

        if (to < from)
            throw new BusinessException(_localizer["Revenue_RangeInvalid"]);

        var toExclusive = to.AddDays(1);
        var groupBy = string.IsNullOrWhiteSpace(query.GroupBy) ? "month" : query.GroupBy.Trim().ToLowerInvariant();

        var rows = (await _revenueRepository.FindAsync(
            r => (r.ConfirmedAt ?? r.CreatedAt) >= from && (r.ConfirmedAt ?? r.CreatedAt) < toExclusive,
            cancellationToken)).ToList();

        var confirmed = rows.Where(r => r.Status == RevenueRecordStatus.Confirmed).ToList();

        return new RevenueStatsResponse
        {
            From = from,
            To = to,
            ConfirmedCount = confirmed.Count,
            DraftCount = rows.Count - confirmed.Count,
            GrossRevenue = confirmed.Sum(r => r.GrossRevenue),
            TaxAmount = confirmed.Sum(r => r.TaxAmount),
            NetRevenue = confirmed.Sum(r => r.NetRevenue),
            TotalCommission = confirmed.Sum(r => r.TotalCommission),
            ExtraCost = confirmed.Sum(r => r.ExtraCost),
            ActualRevenue = confirmed.Sum(r => r.ActualRevenue),
            Points = confirmed
                .GroupBy(r => PeriodKey(r.ConfirmedAt ?? r.CreatedAt, groupBy))
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => new RevenueStatsPoint
                {
                    Period = g.Key,
                    GrossRevenue = g.Sum(r => r.GrossRevenue),
                    TaxAmount = g.Sum(r => r.TaxAmount),
                    TotalCommission = g.Sum(r => r.TotalCommission),
                    ExtraCost = g.Sum(r => r.ExtraCost),
                    ActualRevenue = g.Sum(r => r.ActualRevenue)
                })
                .ToList()
        };
    }

    /// <summary>Khoá kỳ thống kê: ngày, tuần (ISO), tháng hay năm.</summary>
    private static string PeriodKey(DateTime moment, string groupBy) => groupBy switch
    {
        "day" => moment.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        "week" => string.Format(CultureInfo.InvariantCulture, "{0}-W{1:D2}", ISOWeek.GetYear(moment), ISOWeek.GetWeekOfYear(moment)),
        "year" => moment.ToString("yyyy", CultureInfo.InvariantCulture),
        _ => moment.ToString("yyyy-MM", CultureInfo.InvariantCulture)
    };

    private void Validate(SaveTransactionRevenueRequest request, decimal taxPercent)
    {
        if (request.GrossRevenue < 0)
            throw new BusinessException(_localizer["Revenue_AmountInvalid"]);
        if (request.ExtraCost < 0)
            throw new BusinessException(_localizer["Revenue_CostInvalid"]);
        if (taxPercent is < 0 or > 100)
            throw new BusinessException(_localizer["Revenue_RateInvalid"]);

        var beneficiaries = request.Commissions.Select(c => c.Beneficiary).ToList();
        if (beneficiaries.Count != beneficiaries.Distinct().Count())
            throw new BusinessException(_localizer["Revenue_DuplicateBeneficiary"]);

        if (request.Commissions.Any(c => c.RatePercent is < 0 or > 100))
            throw new BusinessException(_localizer["Revenue_RateInvalid"]);
    }

    private static RevenueCalculator.RevenueBreakdown Calculate(SaveTransactionRevenueRequest request, bool taxIncluded, decimal taxPercent)
        => RevenueCalculator.Calculate(
            request.GrossRevenue,
            taxIncluded,
            taxPercent,
            request.Commissions.Select(c => new RevenueCalculator.CommissionRate(c.Beneficiary, c.RatePercent)),
            request.ExtraCost);

    /// <summary>Ghi số đã tính lên bản khai và đồng bộ danh sách hoa hồng từng bên.</summary>
    private void Apply(
        TransactionRevenue entity,
        SaveTransactionRevenueRequest request,
        bool taxIncluded,
        decimal taxPercent,
        RevenueCalculator.RevenueBreakdown breakdown)
    {
        entity.ReferenceCode = request.ReferenceCode;
        entity.GrossRevenue = breakdown.GrossRevenue;
        entity.TaxIncluded = taxIncluded;
        entity.TaxRate = taxPercent;
        entity.TaxAmount = breakdown.TaxAmount;
        entity.NetRevenue = breakdown.NetRevenue;
        entity.TotalCommission = breakdown.TotalCommission;
        entity.ExtraCost = breakdown.ExtraCost;
        entity.ExtraCostNote = request.ExtraCostNote;
        entity.ActualRevenue = breakdown.ActualRevenue;

        var tracked = entity.Commissions.Where(c => !c.IsDeleted).ToList();

        foreach (var line in breakdown.Commissions)
        {
            var source = request.Commissions.First(c => c.Beneficiary == line.Beneficiary);
            var item = tracked.FirstOrDefault(c => c.Beneficiary == line.Beneficiary);

            if (item is null)
            {
                entity.Commissions.Add(new TransactionCommission
                {
                    TransactionRevenueId = entity.Id,
                    Beneficiary = line.Beneficiary,
                    UserId = source.UserId,
                    Rate = line.RatePercent,
                    Amount = line.Amount
                });
                continue;
            }

            item.UserId = source.UserId;
            item.Rate = line.RatePercent;
            item.Amount = line.Amount;
        }

        // Bên bị bỏ khỏi bản khai thì ẩn đi, không xoá cứng để còn dấu vết.
        foreach (var item in tracked.Where(c => breakdown.Commissions.All(l => l.Beneficiary != c.Beneficiary)))
            _commissionRepository.Delete(item);
    }

    private static TransactionRevenueResponse Map(TransactionRevenue entity) => new()
    {
        Id = entity.Id,
        Type = entity.Type,
        ReferenceId = entity.ReferenceId,
        ReferenceCode = entity.ReferenceCode,
        GrossRevenue = entity.GrossRevenue,
        TaxIncluded = entity.TaxIncluded,
        TaxPercent = entity.TaxRate,
        TaxAmount = entity.TaxAmount,
        NetRevenue = entity.NetRevenue,
        Commissions = entity.Commissions
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Beneficiary)
            .Select(c => new TransactionCommissionResponse
            {
                Beneficiary = c.Beneficiary,
                UserId = c.UserId,
                RatePercent = c.Rate,
                Amount = c.Amount
            })
            .ToList(),
        TotalCommission = entity.TotalCommission,
        ExtraCost = entity.ExtraCost,
        ExtraCostNote = entity.ExtraCostNote,
        ActualRevenue = entity.ActualRevenue,
        Status = entity.Status,
        ConfirmedBy = entity.ConfirmedBy,
        ConfirmedAt = entity.ConfirmedAt,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
