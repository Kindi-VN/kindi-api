namespace Kindi.API.Application.DTOs.responses;

using Kindi.API.Domain.Enums;

/// <summary>Một lần chi trả hoa hồng.</summary>
public class PayoutStatementResponse
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>Tên đăng nhập của thành viên.</summary>
    public string? Username { get; set; }

    /// <summary>Họ tên của thành viên.</summary>
    public string? FullName { get; set; }

    /// <summary>Chi trả theo kỳ tháng hay rút sớm.</summary>
    public PayoutType Type { get; set; }

    /// <summary>Hoa hồng ghi nhận trong kỳ (hoặc số tiền yêu cầu rút với kiểu rút sớm).</summary>
    public decimal AccruedAmount { get; set; }

    /// <summary>Phí rút sớm áp dụng (%).</summary>
    public decimal FeeRate { get; set; }

    /// <summary>Tiền phí rút sớm.</summary>
    public decimal FeeAmount { get; set; }

    /// <summary>Số tiền thực nhận.</summary>
    public decimal NetAmount { get; set; }

    /// <summary>Trạng thái chi trả.</summary>
    public PayoutStatus Status { get; set; }

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

    /// <summary>Lý do từ chối hoặc ghi chú.</summary>
    public string? Note { get; set; }

    /// <summary>Kỳ giải ngân của lần chi trả này.</summary>
    public Guid PayoutPeriodId { get; set; }

    /// <summary>Nhãn kỳ giải ngân (MM/yyyy).</summary>
    public string PeriodLabel { get; set; } = string.Empty;

    /// <summary>Thời điểm tạo.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>Một kỳ giải ngân theo tháng.</summary>
public class PayoutPeriodResponse
{
    public Guid Id { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    /// <summary>Nhãn kỳ (MM/yyyy).</summary>
    public string PeriodLabel { get; set; } = string.Empty;

    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public PayoutPeriodStatus Status { get; set; }

    public DateTime? ClosedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    /// <summary>Ngày dự kiến chi trả (đầu tháng sau).</summary>
    public DateTime? PayoutDate { get; set; }

    public string? Note { get; set; }

    /// <summary>Số lần chi trả trong kỳ.</summary>
    public int PayoutCount { get; set; }

    /// <summary>Tổng tiền thực nhận của kỳ.</summary>
    public decimal TotalNetAmount { get; set; }
}

/// <summary>Cấu hình phí rút sớm dùng chung.</summary>
public class PayoutSettingResponse
{
    /// <summary>Cho phép thành viên gửi yêu cầu rút sớm.</summary>
    public bool IsEarlyWithdrawalEnabled { get; set; }

    /// <summary>Phí rút sớm mặc định (%).</summary>
    public decimal EarlyWithdrawalFeeRate { get; set; }

    /// <summary>Phí rút sớm tối thiểu.</summary>
    public decimal? MinEarlyWithdrawalFee { get; set; }

    /// <summary>Phí rút sớm tối đa.</summary>
    public decimal? MaxEarlyWithdrawalFee { get; set; }

    /// <summary>Số tiền rút tối thiểu.</summary>
    public decimal MinWithdrawalAmount { get; set; }

    /// <summary>Ngày chốt sổ trong tháng (0 = ngày cuối tháng).</summary>
    public int ClosingDay { get; set; }

    /// <summary>Ghi chú nội bộ.</summary>
    public string? Note { get; set; }
}

/// <summary>Ví hoa hồng của chính thành viên đang đăng nhập.</summary>
public class MyWalletResponse
{
    /// <summary>Hoa hồng đã đối soát và chưa nằm trong kỳ chi trả nào — có thể rút sớm.</summary>
    public decimal AvailableAmount { get; set; }

    /// <summary>Hoa hồng đang chờ duyệt chi trả (gồm cả yêu cầu rút sớm đang chờ).</summary>
    public decimal PendingAmount { get; set; }

    /// <summary>Tổng hoa hồng đã chi trả.</summary>
    public decimal PaidAmount { get; set; }

    /// <summary>Đã rút sớm trong tháng hiện tại.</summary>
    public decimal WithdrawnThisMonth { get; set; }

    /// <summary>Hạn mức rút sớm trong tháng theo hạng; trống là không giới hạn riêng.</summary>
    public decimal? MonthlyLimit { get; set; }

    /// <summary>Hạn mức còn lại trong tháng; trống khi không giới hạn riêng.</summary>
    public decimal? RemainingLimit { get; set; }

    /// <summary>Phí rút sớm đang áp cho tài khoản (%).</summary>
    public decimal EarlyWithdrawalFeeRate { get; set; }

    /// <summary>Phí rút sớm tối thiểu.</summary>
    public decimal? MinEarlyWithdrawalFee { get; set; }

    /// <summary>Phí rút sớm tối đa.</summary>
    public decimal? MaxEarlyWithdrawalFee { get; set; }

    /// <summary>Số tiền rút tối thiểu.</summary>
    public decimal MinWithdrawalAmount { get; set; }

    /// <summary>Cho phép rút sớm.</summary>
    public bool IsEarlyWithdrawalEnabled { get; set; }

    /// <summary>Đã có thông tin ngân hàng.</summary>
    public bool HasBankAccount { get; set; }

    /// <summary>Thông tin ngân hàng đã được xác minh.</summary>
    public bool BankAccountVerified { get; set; }

    /// <summary>Hạng thành viên hiện tại.</summary>
    public MyMembershipResponse Membership { get; set; } = new();

    /// <summary>Kỳ giải ngân đang mở.</summary>
    public PayoutPeriodResponse? CurrentPeriod { get; set; }

    /// <summary>Các lần chi trả gần đây của tài khoản.</summary>
    public List<PayoutStatementResponse> RecentPayouts { get; set; } = new();
}
