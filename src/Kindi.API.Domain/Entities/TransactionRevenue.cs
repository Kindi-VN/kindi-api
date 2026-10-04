// TransactionRevenue.cs
using Kindi.API.Domain.Enums;

namespace Kindi.API.Domain.Entities;

/// <summary>
/// Doanh thu của một giao dịch do quản trị viên khai sau khi đã thoả thuận với các bên. Số nhập vào là
/// doanh thu gộp; hệ thống trừ thuế, trừ hoa hồng từng bên nhận và trừ chi phí phát sinh để ra số thực
/// nhận. Mọi tỷ lệ dùng lúc khai được lưu lại trên bản ghi, nên đổi cấu hình sau này không làm lệch số cũ.
/// </summary>
public class TransactionRevenue : BaseEntity
{
    /// <summary>Loại giao dịch được khai.</summary>
    public TransactionType Type { get; set; }

    /// <summary>Id của giao dịch.</summary>
    public Guid ReferenceId { get; set; }

    /// <summary>Mã giao dịch hiển thị (mã yêu cầu mua hàng / mã yêu cầu mua chung).</summary>
    public string ReferenceCode { get; set; } = string.Empty;

    /// <summary>Doanh thu gộp quản trị viên nhập.</summary>
    public decimal GrossRevenue { get; set; }

    /// <summary>Số đã nhập đã bao gồm thuế hay chưa.</summary>
    public bool TaxIncluded { get; set; }

    /// <summary>Tỷ lệ thuế (%) áp dụng.</summary>
    public decimal TaxRate { get; set; }

    /// <summary>Tiền thuế.</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>Doanh thu sau thuế — cơ sở tính hoa hồng các bên.</summary>
    public decimal NetRevenue { get; set; }

    /// <summary>Tổng hoa hồng đã chia cho các bên.</summary>
    public decimal TotalCommission { get; set; }

    /// <summary>Chi phí phát sinh ngoài thuế và hoa hồng.</summary>
    public decimal ExtraCost { get; set; }

    /// <summary>Ghi chú cho chi phí phát sinh.</summary>
    public string? ExtraCostNote { get; set; }

    /// <summary>Số thực nhận sau khi trừ thuế, hoa hồng và chi phí.</summary>
    public decimal ActualRevenue { get; set; }

    /// <summary>Trạng thái bản khai.</summary>
    public RevenueRecordStatus Status { get; set; } = RevenueRecordStatus.Draft;

    /// <summary>Tài khoản chốt số liệu.</summary>
    public string? ConfirmedBy { get; set; }

    /// <summary>Thời điểm chốt số liệu.</summary>
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>Hoa hồng chia cho từng bên trong giao dịch này.</summary>
    public ICollection<TransactionCommission> Commissions { get; set; } = new List<TransactionCommission>();

    /// <summary>Các dòng chi phí phát sinh của giao dịch; tổng các dòng là <see cref="ExtraCost"/>.</summary>
    public ICollection<TransactionExpense> Expenses { get; set; } = new List<TransactionExpense>();
}
