namespace Kindi.API.Application.DTOs.responses;

/// <summary>Một loại chi phí dùng chung kèm các loại giao dịch được gắn (rỗng = loại mặc định).</summary>
public class RevenueExpenseTypeResponse
{
    public Guid Id { get; set; }

    /// <summary>Tên loại chi phí.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Thứ tự sắp xếp.</summary>
    public int SortOrder { get; set; }

    /// <summary>Các loại giao dịch được gắn (giá trị số của TransactionType).</summary>
    public List<int> TransactionTypes { get; set; } = new();

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Một loại chi phí đã giải theo loại giao dịch, dùng cho màn khai doanh thu.</summary>
public class RevenueExpenseTypeResolveResponse
{
    public Guid Id { get; set; }

    /// <summary>Tên loại chi phí.</summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>Hai cột cấu hình thuế doanh thu nằm trong bảng cài đặt chung.</summary>
public class RevenueExpenseConfigResponse
{
    /// <summary>Tỷ lệ thuế doanh thu (%) mặc định; bỏ trống khi khai thì lấy giá trị này.</summary>
    public decimal RevenueTaxPercent { get; set; }

    /// <summary>Số doanh thu nhập vào đã gồm thuế hay chưa.</summary>
    public bool RevenueTaxIncluded { get; set; }
}
