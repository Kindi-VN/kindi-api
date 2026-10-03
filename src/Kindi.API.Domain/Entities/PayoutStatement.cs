// PayoutStatement.cs
using Kindi.API.Domain.Enums;

namespace Kindi.API.Domain.Entities;

/// <summary>
/// Một lần chi trả hoa hồng cho một tài khoản trong một kỳ: chi trả theo kỳ tháng (mặc định)
/// hoặc rút sớm theo yêu cầu. Thông tin ngân hàng được chụp lại tại thời điểm tạo để đối chiếu về sau.
/// </summary>
public class PayoutStatement : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid PayoutPeriodId { get; set; }

    /// <summary>Chi trả theo kỳ tháng hay rút sớm.</summary>
    public PayoutType Type { get; set; }

    /// <summary>Hoa hồng ghi nhận trong kỳ (hoặc số tiền thành viên yêu cầu rút với kiểu rút sớm).</summary>
    public decimal AccruedAmount { get; set; }

    /// <summary>Phí rút sớm áp dụng (% trên số tiền rút); bằng 0 với chi trả theo kỳ.</summary>
    public decimal FeeRate { get; set; }

    /// <summary>Tiền phí rút sớm.</summary>
    public decimal FeeAmount { get; set; }

    /// <summary>Số tiền thực nhận sau phí.</summary>
    public decimal NetAmount { get; set; }

    /// <summary>Trạng thái chi trả.</summary>
    public PayoutStatus Status { get; set; } = PayoutStatus.Pending;

    /// <summary>Thông tin ngân hàng tại thời điểm tạo (chụp lại).</summary>
    public string? BankName { get; set; }
    public string? BankBranch { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankAccountHolder { get; set; }

    /// <summary>Thời điểm thành viên gửi yêu cầu rút sớm.</summary>
    public DateTime? RequestedAt { get; set; }

    /// <summary>Thời điểm quản trị viên xử lý.</summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>Người xử lý.</summary>
    public string? ProcessedBy { get; set; }

    /// <summary>Lý do từ chối hoặc ghi chú nội bộ.</summary>
    public string? Note { get; set; }

    // Navigation
    public virtual User User { get; set; } = null!;
    public virtual PayoutPeriod PayoutPeriod { get; set; } = null!;
}
