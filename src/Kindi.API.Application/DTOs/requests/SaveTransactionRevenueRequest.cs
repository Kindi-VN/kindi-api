namespace Kindi.API.Application.DTOs.requests;

using Kindi.API.Domain.Enums;

/// <summary>Một bên nhận hoa hồng trong bản khai doanh thu.</summary>
public class TransactionCommissionRequest
{
    /// <summary>Bên nhận: người giới thiệu hay đối tác.</summary>
    public CommissionBeneficiary Beneficiary { get; set; }

    /// <summary>Tài khoản nhận, nếu đã xác định được.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Tỷ lệ hoa hồng (%) tính trên doanh thu sau thuế.</summary>
    public decimal RatePercent { get; set; }
}

/// <summary>Một dòng chi phí phát sinh trong bản khai doanh thu.</summary>
public class TransactionExpenseRequest
{
    /// <summary>Tên chi phí.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Số tiền chi phí.</summary>
    public decimal Amount { get; set; }
}

/// <summary>Khai doanh thu cho một giao dịch.</summary>
public class SaveTransactionRevenueRequest
{
    /// <summary>Loại giao dịch được khai.</summary>
    public TransactionType Type { get; set; }

    /// <summary>Id giao dịch.</summary>
    public Guid ReferenceId { get; set; }

    /// <summary>Mã giao dịch hiển thị.</summary>
    public string ReferenceCode { get; set; } = string.Empty;

    /// <summary>Doanh thu gộp quản trị viên nhập.</summary>
    public decimal GrossRevenue { get; set; }

    /// <summary>Số nhập đã gồm thuế hay chưa; bỏ trống thì lấy theo cài đặt chung.</summary>
    public bool? TaxIncluded { get; set; }

    /// <summary>Tỷ lệ thuế (%); bỏ trống thì lấy theo cài đặt chung.</summary>
    public decimal? TaxPercent { get; set; }

    /// <summary>Chi phí phát sinh; giữ lại để tương thích client cũ, không còn dùng — chi phí lấy từ <see cref="Expenses"/>.</summary>
    public decimal ExtraCost { get; set; }

    /// <summary>Ghi chú cho chi phí phát sinh; giữ lại để tương thích client cũ, không còn dùng.</summary>
    public string? ExtraCostNote { get; set; }

    /// <summary>Các dòng chi phí phát sinh; chi phí phát sinh của bản khai bằng tổng các dòng này.</summary>
    public List<TransactionExpenseRequest> Expenses { get; set; } = new();

    /// <summary>Hoa hồng từng bên nhận.</summary>
    public List<TransactionCommissionRequest> Commissions { get; set; } = new();
}
