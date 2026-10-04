namespace Kindi.API.Application.DTOs.requests;

using Kindi.API.Domain.Enums;
using Kindi.API.Application.DTOs;

/// <summary>Lọc danh sách bản khai doanh thu.</summary>
public class RevenueQueryDto : PaginationDto
{
    /// <summary>Tìm theo mã giao dịch.</summary>
    public string? Keyword { get; set; }

    /// <summary>Chỉ lấy một loại giao dịch.</summary>
    public TransactionType? Type { get; set; }

    /// <summary>Chỉ lấy bản khai ở một trạng thái.</summary>
    public RevenueRecordStatus? Status { get; set; }
}

/// <summary>Khoảng thời gian và cách gộp nhóm cho thống kê doanh thu.</summary>
public class RevenueStatsQueryDto
{
    /// <summary>Từ ngày (bỏ trống thì lấy từ đầu tháng hiện tại).</summary>
    public DateTime? From { get; set; }

    /// <summary>Đến ngày (bỏ trống thì lấy đến hết ngày hiện tại).</summary>
    public DateTime? To { get; set; }

    /// <summary>Cách gộp nhóm: day, week, month (mặc định) hay year.</summary>
    public string? GroupBy { get; set; }
}
