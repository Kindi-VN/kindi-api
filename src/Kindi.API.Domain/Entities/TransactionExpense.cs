// TransactionExpense.cs
namespace Kindi.API.Domain.Entities;

/// <summary>
/// Một dòng chi phí của bản khai doanh thu. Tên và số tiền được chốt vào từng bản khai nên đổi cấu
/// hình loại chi phí sau này không làm đổi số của bản khai cũ; tổng các dòng chính là chi phí phát sinh
/// của giao dịch.
/// </summary>
public class TransactionExpense : BaseEntity
{
    /// <summary>Bản khai doanh thu mà dòng chi phí này thuộc về.</summary>
    public Guid TransactionRevenueId { get; set; }

    /// <summary>Bản khai doanh thu.</summary>
    public TransactionRevenue TransactionRevenue { get; set; } = null!;

    /// <summary>Tên chi phí (lấy từ loại chi phí hoặc quản trị viên nhập).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Số tiền chi phí.</summary>
    public decimal Amount { get; set; }

    /// <summary>Thứ tự sắp xếp các dòng trong bản khai.</summary>
    public int SortOrder { get; set; }
}
