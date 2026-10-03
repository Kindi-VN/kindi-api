namespace Kindi.API.Application.DTOs.requests;

/// <summary>Lọc thống kê giới thiệu. Mặc định lấy 30 ngày gần nhất.</summary>
public class ReferralStatsQueryDto
{
    /// <summary>Từ ngày (mặc định: 30 ngày trước).</summary>
    public DateTime? From { get; set; }

    /// <summary>Đến ngày (mặc định: hiện tại).</summary>
    public DateTime? To { get; set; }

    /// <summary>Tìm theo mã chia sẻ hoặc tên chủ mã.</summary>
    public string? Search { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
