// PayoutPeriod.cs
using Kindi.API.Domain.Enums;

namespace Kindi.API.Domain.Entities;

/// <summary>
/// Kỳ giải ngân theo tháng: hoa hồng phát sinh trong tháng được chốt sổ cuối tháng
/// và chi trả vào đầu tháng sau.
/// </summary>
public class PayoutPeriod : BaseEntity
{
    /// <summary>Năm của kỳ.</summary>
    public int Year { get; set; }

    /// <summary>Tháng của kỳ (1-12).</summary>
    public int Month { get; set; }

    /// <summary>Ngày bắt đầu kỳ.</summary>
    public DateTime FromDate { get; set; }

    /// <summary>Ngày kết thúc kỳ (ngày chốt sổ).</summary>
    public DateTime ToDate { get; set; }

    /// <summary>Trạng thái kỳ.</summary>
    public PayoutPeriodStatus Status { get; set; } = PayoutPeriodStatus.Open;

    /// <summary>Thời điểm chốt sổ.</summary>
    public DateTime? ClosedAt { get; set; }

    /// <summary>Thời điểm chi trả xong.</summary>
    public DateTime? PaidAt { get; set; }

    /// <summary>Ngày dự kiến chi trả (đầu tháng sau).</summary>
    public DateTime? PayoutDate { get; set; }

    /// <summary>Ghi chú nội bộ.</summary>
    public string? Note { get; set; }

    // Navigation
    public virtual ICollection<PayoutStatement> Statements { get; set; } = new List<PayoutStatement>();
}
