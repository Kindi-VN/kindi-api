namespace Kindi.API.Application.DTOs.responses;

using Kindi.API.Domain.Enums;

/// <summary>Hoa hồng của một bên trong bản khai doanh thu.</summary>
public class TransactionCommissionResponse
{
    /// <summary>Bên nhận hoa hồng.</summary>
    public CommissionBeneficiary Beneficiary { get; set; }

    /// <summary>Tài khoản nhận, nếu đã xác định.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Tỷ lệ hoa hồng (%) đã áp.</summary>
    public decimal RatePercent { get; set; }

    /// <summary>Số tiền hoa hồng.</summary>
    public decimal Amount { get; set; }
}

/// <summary>Một dòng chi phí phát sinh trong bản khai doanh thu.</summary>
public class TransactionExpenseResponse
{
    /// <summary>Tên chi phí.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Số tiền chi phí.</summary>
    public decimal Amount { get; set; }
}

/// <summary>Bản khai doanh thu của một giao dịch, kèm số đã bóc tách.</summary>
public class TransactionRevenueResponse
{
    public Guid Id { get; set; }

    public TransactionType Type { get; set; }

    public Guid ReferenceId { get; set; }

    /// <summary>Mã giao dịch hiển thị.</summary>
    public string ReferenceCode { get; set; } = string.Empty;

    /// <summary>Doanh thu gộp đã nhập.</summary>
    public decimal GrossRevenue { get; set; }

    /// <summary>Số nhập có gồm thuế hay không.</summary>
    public bool TaxIncluded { get; set; }

    /// <summary>Tỷ lệ thuế (%) đã áp.</summary>
    public decimal TaxPercent { get; set; }

    /// <summary>Tiền thuế.</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>Doanh thu sau thuế — cơ sở tính hoa hồng.</summary>
    public decimal NetRevenue { get; set; }

    /// <summary>Hoa hồng từng bên đã chia.</summary>
    public List<TransactionCommissionResponse> Commissions { get; set; } = new();

    /// <summary>Tổng hoa hồng các bên.</summary>
    public decimal TotalCommission { get; set; }

    /// <summary>Chi phí phát sinh.</summary>
    public decimal ExtraCost { get; set; }

    /// <summary>Ghi chú chi phí phát sinh.</summary>
    public string? ExtraCostNote { get; set; }

    /// <summary>Các dòng chi phí phát sinh của bản khai.</summary>
    public List<TransactionExpenseResponse> Expenses { get; set; } = new();

    /// <summary>Số thực nhận sau thuế, hoa hồng và chi phí.</summary>
    public decimal ActualRevenue { get; set; }

    /// <summary>Trạng thái bản khai.</summary>
    public RevenueRecordStatus Status { get; set; }

    /// <summary>Người chốt số liệu.</summary>
    public string? ConfirmedBy { get; set; }

    /// <summary>Thời điểm chốt.</summary>
    public DateTime? ConfirmedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Một mốc trong thống kê doanh thu.</summary>
public class RevenueStatsPoint
{
    /// <summary>Khoá kỳ: 2026, 2026-10, 2026-10-04 hoặc 2026-W41.</summary>
    public string Period { get; set; } = string.Empty;

    public decimal GrossRevenue { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal ExtraCost { get; set; }
    public decimal ActualRevenue { get; set; }
}

/// <summary>Thống kê doanh thu trong một khoảng thời gian, chỉ tính bản khai đã chốt.</summary>
public class RevenueStatsResponse
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    /// <summary>Số bản khai đã chốt trong kỳ.</summary>
    public int ConfirmedCount { get; set; }

    /// <summary>Số bản khai còn nháp trong kỳ (không tính vào tổng).</summary>
    public int DraftCount { get; set; }

    public decimal GrossRevenue { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal NetRevenue { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal ExtraCost { get; set; }
    public decimal ActualRevenue { get; set; }

    /// <summary>Chi tiết theo kỳ đã chọn.</summary>
    public List<RevenueStatsPoint> Points { get; set; } = new();
}
