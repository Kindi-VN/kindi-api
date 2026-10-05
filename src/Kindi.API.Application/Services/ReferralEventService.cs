using Kindi.API.Application.Common.Extensions;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Kindi.API.Application.Services;

/// <summary>
/// Thống kê tình hình giới thiệu theo mã chia sẻ: số tài khoản được mời, số đơn mua chung,
/// yêu cầu tìm hàng / offer, lượt vào nhóm, đăng ký đối tác — đọc từ bảng ReferralEvents.
/// </summary>
public class ReferralEventService : IReferralEventService
{
    private readonly IQueryService _queryService;
    private readonly IReferralService _referralService;

    public ReferralEventService(IQueryService queryService, IReferralService referralService)
    {
        _queryService = queryService;
        _referralService = referralService;
    }

    public async Task<PagedList<ReferralStatsItemDto>> GetStatsAsync(ReferralStatsQueryDto query)
    {
        var items = await BuildStatsAsync(query, null);
        var page = query.Page < 1 ? 1 : query.Page;
        var size = query.PageSize < 1 ? 10 : query.PageSize;

        return new PagedList<ReferralStatsItemDto>(
            items.Skip((page - 1) * size).Take(size).ToList(),
            items.Count,
            page,
            size);
    }

    public async Task<ReferralStatsOverviewDto> GetOverviewAsync(ReferralStatsQueryDto query)
    {
        var items = await BuildStatsAsync(query, null);
        return await BuildOverviewAsync(query, items, null);
    }

    public async Task<ReferralStatsOverviewDto> GetMyStatsAsync(ReferralStatsQueryDto query)
    {
        var referralCode = await _referralService.GetSharerReferralCodeAsync();
        if (string.IsNullOrWhiteSpace(referralCode))
            return await BuildOverviewAsync(query, new List<ReferralStatsItemDto>(), null);

        var code = referralCode.Trim().ToUpperInvariant();
        var items = await BuildStatsAsync(query, code);
        return await BuildOverviewAsync(query, items, code);
    }

    public Task<PagedList<ReferralEventResponseDto>> GetEventsAsync(string referralCode, ReferralEventQueryDto query)
    {
        var code = (referralCode ?? string.Empty).Trim().ToUpperInvariant();
        return GetEventsForCodeAsync(code, query);
    }

    public async Task<PagedList<ReferralEventResponseDto>> GetMyEventsAsync(ReferralEventQueryDto query)
    {
        // Chỉ lấy mã của chính tài khoản đang đăng nhập, không nhận mã từ client.
        var referralCode = await _referralService.GetSharerReferralCodeAsync();
        if (string.IsNullOrWhiteSpace(referralCode))
            return new PagedList<ReferralEventResponseDto>(new List<ReferralEventResponseDto>(), 0, query.Page, query.PageSize);

        return await GetEventsForCodeAsync(referralCode.Trim().ToUpperInvariant(), query);
    }

    private async Task<PagedList<ReferralEventResponseDto>> GetEventsForCodeAsync(string code, ReferralEventQueryDto query)
    {
        var (from, to) = Range(query.From, query.To);

        var eventsQuery = _queryService.GetAllNoTracking<ReferralEvent>()
            .Where(e => e.RecordReferrerCode == code)
            .Where(e => e.CreatedAt >= from && e.CreatedAt <= to)
            .WhereIf(query.EventType.HasValue, e => e.EventType == query.EventType!.Value);

        // Từ khoá: searchField chỉ định thì CHỈ dò đúng cột đó; bỏ trống thì giữ nguyên hành vi cũ.
        eventsQuery = ApplySearch(eventsQuery, query.Search, query.SearchField);

        var paged = await eventsQuery
            .OrderByDescending(e => e.CreatedAt)
            .ToPagedListAsync(query.Page, query.PageSize, null, null, defaultSortBy: "CreatedAt");

        var items = paged.Items.Select(e => new ReferralEventResponseDto
        {
            Id = e.Id,
            ReferralEventCode = e.ReferralEventCode,
            RecordReferrerCode = e.RecordReferrerCode,
            ReferredUserId = e.ReferredUserId,
            EventType = e.EventType,
            RefEntityId = e.RefEntityId,
            RefEntityCode = e.RefEntityCode,
            Amount = e.Amount,
            CommissionAmount = e.CommissionAmount,
            Status = e.Status,
            IsGuestAccount = e.IsGuestAccount,
            CreatedAt = e.CreatedAt
        }).ToList();

        // Tên chủ mã + tên người được giới thiệu (thông tin cá nhân nằm ở bảng Users).
        var names = await _referralService.LoadNamesAsync(new[] { code });
        names.TryGetValue(code, out var referrerName);

        var userIds = items.Select(i => i.ReferredUserId).Distinct().ToList();
        var users = await _queryService.GetListAsync<User>(u => userIds.Contains(u.Id));
        var userNames = users.ToDictionary(u => u.Id, u => u.FullName);

        foreach (var item in items)
        {
            item.ReferrerName = referrerName;
            item.ReferredUserName = userNames.TryGetValue(item.ReferredUserId, out var name) ? name : null;
        }

        return new PagedList<ReferralEventResponseDto>(items, paged.TotalCount, paged.PageNumber, paged.PageSize);
    }

    // ==================== HELPERS ====================

    private static (DateTime From, DateTime To) Range(DateTime? from, DateTime? to)
    {
        var start = from ?? DateTime.UtcNow.AddDays(-30);
        var end = to ?? DateTime.UtcNow;
        return start <= end ? (start, end) : (end, start);
    }

    /// <summary>
    /// Lọc phát sinh theo từ khoá. Có <paramref name="searchField"/> thì CHỈ dò đúng cột đó;
    /// bỏ trống thì giữ nguyên hành vi cũ (không lọc theo từ khoá). Cột EventType/Status khớp theo
    /// tên hoặc số của enum, không khớp một phần.
    /// </summary>
    private static IQueryable<ReferralEvent> ApplySearch(
        IQueryable<ReferralEvent> query, string? search, ReferralEventSearchField? searchField)
    {
        if (!searchField.HasValue)
            return query;

        var keyword = search?.Trim();
        if (string.IsNullOrEmpty(keyword))
            return query;

        var pattern = $"%{keyword}%";

        return searchField.Value switch
        {
            ReferralEventSearchField.ReferralCode =>
                query.Where(e => e.RecordReferrerCode != null && EF.Functions.ILike(e.RecordReferrerCode, pattern)),
            ReferralEventSearchField.RefEntityCode =>
                query.Where(e => e.RefEntityCode != null && EF.Functions.ILike(e.RefEntityCode, pattern)),
            ReferralEventSearchField.EventType =>
                Enum.TryParse<ReferralEventType>(keyword, true, out var eventType)
                    ? query.Where(e => e.EventType == eventType)
                    : query.Where(e => false),
            ReferralEventSearchField.Status =>
                Enum.TryParse<ReferralEventStatus>(keyword, true, out var status)
                    ? query.Where(e => e.Status == status)
                    : query.Where(e => false),
            _ => query
        };
    }

    private async Task<ReferralStatsOverviewDto> BuildOverviewAsync(
        ReferralStatsQueryDto query, List<ReferralStatsItemDto> items, string? onlyCode)
    {
        var (from, to) = Range(query.From, query.To);

        return new ReferralStatsOverviewDto
        {
            From = from,
            To = to,
            Summary = Summarize(items),
            Timeline = await BuildTimelineAsync(from, to, onlyCode)
        };
    }

    /// <summary>Gộp phát sinh theo mã chia sẻ (lọc theo khoảng thời gian + từ khoá).</summary>
    private async Task<List<ReferralStatsItemDto>> BuildStatsAsync(ReferralStatsQueryDto query, string? onlyCode)
    {
        var (from, to) = Range(query.From, query.To);

        var items = await _queryService.GetAllNoTracking<ReferralEvent>()
            .Where(e => e.CreatedAt >= from && e.CreatedAt <= to)
            .WhereIf(!string.IsNullOrWhiteSpace(onlyCode), e => e.RecordReferrerCode == onlyCode)
            .GroupBy(e => e.RecordReferrerCode)
            .Select(g => new ReferralStatsItemDto
            {
                RecordReferrerCode = g.Key,
                // Đếm theo NGƯỜI được giới thiệu, không đếm theo lượt và không tính tài khoản khách.
                // Phát sinh đã huỷ/bị từ chối bị loại khỏi mọi cột — huỷ thì trừ ra, tham gia lại thì tính lại.
                ReferredUsers = g
                    .Where(e => !e.IsGuestAccount && e.Status != ReferralEventStatus.Cancelled && e.Status != ReferralEventStatus.Rejected)
                    .Select(e => e.ReferredUserId).Distinct().Count(),
                ReferredGuestUsers = g
                    .Where(e => e.IsGuestAccount && e.Status != ReferralEventStatus.Cancelled && e.Status != ReferralEventStatus.Rejected)
                    .Select(e => e.ReferredUserId).Distinct().Count(),
                CancelledEvents = g.Sum(e => e.Status == ReferralEventStatus.Cancelled || e.Status == ReferralEventStatus.Rejected ? 1 : 0),
                GroupBuyingRequests = g.Sum(e => e.EventType == ReferralEventType.GroupBuyingRequest
                    && e.Status != ReferralEventStatus.Cancelled && e.Status != ReferralEventStatus.Rejected ? 1 : 0),
                GroupBuyingJoins = g.Sum(e => e.EventType == ReferralEventType.GroupBuyingJoin
                    && e.Status != ReferralEventStatus.Cancelled && e.Status != ReferralEventStatus.Rejected ? 1 : 0),
                PurchaseRequests = g.Sum(e => e.EventType == ReferralEventType.PurchaseRequest
                    && e.Status != ReferralEventStatus.Cancelled && e.Status != ReferralEventStatus.Rejected ? 1 : 0),
                OfferRequests = g.Sum(e => e.EventType == ReferralEventType.OfferRequest
                    && e.Status != ReferralEventStatus.Cancelled && e.Status != ReferralEventStatus.Rejected ? 1 : 0),
                GroupMemberJoins = g.Sum(e => e.EventType == ReferralEventType.GroupMemberJoin
                    && e.Status != ReferralEventStatus.Cancelled && e.Status != ReferralEventStatus.Rejected ? 1 : 0),
                PartnerRegisters = g.Sum(e => e.EventType == ReferralEventType.PartnerRegister
                    && e.Status != ReferralEventStatus.Cancelled && e.Status != ReferralEventStatus.Rejected ? 1 : 0),
                TotalEvents = g.Sum(e => e.Status == ReferralEventStatus.Cancelled || e.Status == ReferralEventStatus.Rejected ? 0 : 1),
                TotalAmount = g.Sum(e => e.Amount ?? 0),
                TotalCommissionAmount = g.Sum(e => e.CommissionAmount ?? 0),
                LastEventAt = g.Max(e => e.CreatedAt)
            })
            .ToListAsync();

        // Tên chủ mã (CTV hoặc tài khoản) — tra theo mã.
        var names = await _referralService.LoadNamesAsync(items.Select(i => i.RecordReferrerCode));
        foreach (var item in items)
            if (names.TryGetValue(item.RecordReferrerCode, out var name))
                item.ReferrerName = name;

        // Tìm theo mã hoặc tên chủ mã: số mã chia sẻ phát sinh trong một khoảng là nhỏ nên lọc ở bộ nhớ.
        var search = query.Search.NormalizeSearchFilter();
        if (search != null)
        {
            var keyword = search.ToLowerInvariant();
            items = items
                .Where(i => i.RecordReferrerCode.ToLowerInvariant().Contains(keyword)
                    || (i.ReferrerName ?? string.Empty).ToLowerInvariant().Contains(keyword))
                .ToList();
        }

        return items
            .OrderByDescending(i => i.TotalEvents)
            .ThenBy(i => i.RecordReferrerCode)
            .ToList();
    }

    private static ReferralStatsSummaryDto Summarize(List<ReferralStatsItemDto> items)
    {
        return new ReferralStatsSummaryDto
        {
            TotalReferrers = items.Count,
            // Cùng một tài khoản được ghi nhận dưới nhiều mã là trường hợp hiếm, chấp nhận đếm cộng dồn.
            TotalReferredUsers = items.Sum(i => i.ReferredUsers),
            TotalReferredGuestUsers = items.Sum(i => i.ReferredGuestUsers),
            TotalCancelledEvents = items.Sum(i => i.CancelledEvents),
            TotalEvents = items.Sum(i => i.TotalEvents),
            TotalGroupBuyingRequests = items.Sum(i => i.GroupBuyingRequests),
            TotalGroupBuyingJoins = items.Sum(i => i.GroupBuyingJoins),
            TotalPurchaseRequests = items.Sum(i => i.PurchaseRequests),
            TotalOfferRequests = items.Sum(i => i.OfferRequests),
            TotalGroupMemberJoins = items.Sum(i => i.GroupMemberJoins),
            TotalPartnerRegisters = items.Sum(i => i.PartnerRegisters),
            TotalAmount = items.Sum(i => i.TotalAmount),
            TotalCommissionAmount = items.Sum(i => i.TotalCommissionAmount)
        };
    }

    /// <summary>Số phát sinh theo ngày trong khoảng đã lọc (khoảng mặc định 30 ngày).</summary>
    private async Task<List<ReferralStatsTimelineItemDto>> BuildTimelineAsync(DateTime from, DateTime to, string? onlyCode)
    {
        var stamps = await _queryService.GetAllNoTracking<ReferralEvent>()
            .Where(e => e.CreatedAt >= from && e.CreatedAt <= to)
            .WhereIf(!string.IsNullOrWhiteSpace(onlyCode), e => e.RecordReferrerCode == onlyCode)
            .Select(e => e.CreatedAt)
            .ToListAsync();

        return stamps
            .GroupBy(d => d.Date)
            .OrderBy(g => g.Key)
            .Select(g => new ReferralStatsTimelineItemDto { Date = g.Key, Count = g.Count() })
            .ToList();
    }
}
