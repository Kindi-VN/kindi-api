using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Domain.Models;

namespace Kindi.API.Application.Common.Interfaces;

/// <summary>Thống kê phát sinh giới thiệu (đọc bảng ReferralEvents).</summary>
public interface IReferralEventService
{
    /// <summary>Thống kê theo từng mã chia sẻ (màn quản trị).</summary>
    Task<PagedList<ReferralStatsItemDto>> GetStatsAsync(ReferralStatsQueryDto query);

    /// <summary>Thẻ tổng quan + biểu đồ theo thời gian (màn quản trị).</summary>
    Task<ReferralStatsOverviewDto> GetOverviewAsync(ReferralStatsQueryDto query);

    /// <summary>Thống kê của mã chia sẻ đang đăng nhập (khu vực thành viên).</summary>
    Task<ReferralStatsOverviewDto> GetMyStatsAsync(ReferralStatsQueryDto query);

    /// <summary>Danh sách phát sinh của một mã chia sẻ (màn quản trị).</summary>
    Task<PagedList<ReferralEventResponseDto>> GetEventsAsync(string referralCode, ReferralEventQueryDto query);
}
