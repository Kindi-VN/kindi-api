namespace Kindi.API.Application.DTOs.requests;

/// <summary>
/// Tạo/cập nhật một loại chi phí dùng chung. Danh sách loại giao dịch rỗng nghĩa là loại mặc định
/// (dùng khi giao dịch không có loại chi phí nào được gắn riêng).
/// </summary>
public class SaveRevenueExpenseTypeRequest
{
    /// <summary>Tên loại chi phí.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Thứ tự sắp xếp; bỏ trống thì mặc định 0.</summary>
    public int SortOrder { get; set; }

    /// <summary>Các loại giao dịch được gắn loại chi phí (giá trị số của TransactionType).</summary>
    public List<int> TransactionTypes { get; set; } = new();
}

/// <summary>Gán hàng loạt loại giao dịch cho một nhóm loại chi phí đã có.</summary>
public class AssignRevenueExpenseTypeScopesRequest
{
    /// <summary>Các loại chi phí cần đặt lại scope.</summary>
    public List<Guid> ExpenseTypeIds { get; set; } = new();

    /// <summary>Danh sách loại giao dịch sẽ gán (rỗng nghĩa là chuyển về mặc định).</summary>
    public List<int> TransactionTypes { get; set; } = new();
}

/// <summary>Cập nhật hai cột cấu hình thuế doanh thu trong bảng cài đặt chung.</summary>
public class RevenueExpenseConfigRequest
{
    /// <summary>Tỷ lệ thuế doanh thu (%) trong khoảng 0-100.</summary>
    public decimal RevenueTaxPercent { get; set; }

    /// <summary>Số doanh thu nhập vào đã gồm thuế hay chưa.</summary>
    public bool RevenueTaxIncluded { get; set; }
}
