// RevenueExpenseType.cs
namespace Kindi.API.Domain.Entities;

/// <summary>
/// Loại chi phí dùng chung khi khai doanh thu giao dịch (ví dụ: vận chuyển, lưu kho, môi giới). Mỗi
/// loại có thể được gắn cho một số loại giao dịch nhất định; loại không gắn loại giao dịch nào là loại
/// mặc định, dùng khi không có loại nào được gắn riêng cho giao dịch đang khai.
/// </summary>
public class RevenueExpenseType : BaseEntity
{
    /// <summary>Tên loại chi phí hiển thị cho quản trị viên.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Thứ tự sắp xếp trong danh sách chọn; số nhỏ hiện trước.</summary>
    public int SortOrder { get; set; }

    /// <summary>Các loại giao dịch được gắn loại chi phí này; rỗng nghĩa là loại mặc định.</summary>
    public ICollection<RevenueExpenseTypeScope> Scopes { get; set; } = new List<RevenueExpenseTypeScope>();
}
