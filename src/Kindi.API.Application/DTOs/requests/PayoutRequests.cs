namespace Kindi.API.Application.DTOs.requests;

using Kindi.API.Application.Common.Models;
using Kindi.API.Domain.Enums;

/// <summary>Yêu cầu rút hoa hồng sớm của thành viên.</summary>
public class CreateWithdrawalRequest
{
    /// <summary>Số tiền muốn rút (trước phí).</summary>
    public decimal Amount { get; set; }
}

/// <summary>Xử lý một lần chi trả (duyệt / từ chối / đã chuyển khoản).</summary>
public class ProcessPayoutRequest
{
    /// <summary>Lý do từ chối hoặc ghi chú nội bộ.</summary>
    public string? Note { get; set; }
}

/// <summary>Cấu hình phí rút sớm dùng chung.</summary>
public class SavePayoutSettingRequest
{
    /// <summary>Cho phép thành viên gửi yêu cầu rút sớm.</summary>
    public bool IsEarlyWithdrawalEnabled { get; set; } = true;

    /// <summary>Phí rút sớm mặc định (% trên số tiền rút).</summary>
    public decimal EarlyWithdrawalFeeRate { get; set; }

    /// <summary>Phí rút sớm tối thiểu của một yêu cầu.</summary>
    public decimal? MinEarlyWithdrawalFee { get; set; }

    /// <summary>Phí rút sớm tối đa của một yêu cầu.</summary>
    public decimal? MaxEarlyWithdrawalFee { get; set; }

    /// <summary>Số tiền rút tối thiểu của một yêu cầu.</summary>
    public decimal MinWithdrawalAmount { get; set; }

    /// <summary>Ngày chốt sổ trong tháng (0 = ngày cuối tháng).</summary>
    public int ClosingDay { get; set; }

    /// <summary>Ghi chú nội bộ.</summary>
    public string? Note { get; set; }
}

/// <summary>Tạo/mở một kỳ giải ngân theo tháng.</summary>
public class CreatePayoutPeriodRequest
{
    /// <summary>Năm của kỳ.</summary>
    public int Year { get; set; }

    /// <summary>Tháng của kỳ (1-12).</summary>
    public int Month { get; set; }

    /// <summary>Ghi chú nội bộ.</summary>
    public string? Note { get; set; }
}

/// <summary>Điều kiện lọc danh sách chi trả hoa hồng.</summary>
public class PayoutQueryDto : PagedRequest
{
    /// <summary>Lọc theo kiểu chi trả (theo kỳ / rút sớm).</summary>
    public PayoutType? Type { get; set; }

    /// <summary>Lọc theo trạng thái.</summary>
    public PayoutStatus? Status { get; set; }

    /// <summary>Lọc theo tài khoản.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Lọc theo kỳ giải ngân.</summary>
    public Guid? PayoutPeriodId { get; set; }

    /// <summary>Tìm theo tên đăng nhập hoặc họ tên.</summary>
    public string? Search { get; set; }
}

/// <summary>Điều kiện lọc danh sách kỳ giải ngân.</summary>
public class PayoutPeriodQueryDto : PagedRequest
{
    /// <summary>Lọc theo năm.</summary>
    public int? Year { get; set; }

    /// <summary>Lọc theo trạng thái kỳ.</summary>
    public PayoutPeriodStatus? Status { get; set; }
}
