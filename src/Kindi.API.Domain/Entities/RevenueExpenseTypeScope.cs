// RevenueExpenseTypeScope.cs
namespace Kindi.API.Domain.Entities;

using Kindi.API.Domain.Enums;

/// <summary>
/// Một loại giao dịch mà loại chi phí được gắn vào. Loại chi phí không có dòng scope nào là loại mặc
/// định; có scope thì chỉ dùng cho đúng các loại giao dịch đó.
/// </summary>
public class RevenueExpenseTypeScope : BaseEntity
{
    /// <summary>Loại chi phí mà scope này thuộc về.</summary>
    public Guid RevenueExpenseTypeId { get; set; }

    /// <summary>Loại chi phí.</summary>
    public RevenueExpenseType RevenueExpenseType { get; set; } = null!;

    /// <summary>Loại giao dịch được gắn loại chi phí.</summary>
    public TransactionType TransactionType { get; set; }
}
