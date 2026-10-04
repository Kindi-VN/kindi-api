namespace Kindi.API.Domain.Rules;

using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;

/// <summary>
/// Luật chọn nhóm loại chi phí dùng cho một loại giao dịch: nếu có ít nhất một loại chi phí được gắn
/// đúng loại giao dịch đó thì chỉ dùng những loại được gắn riêng; nếu không có loại nào gắn riêng thì
/// dùng nhóm loại mặc định (loại không gắn loại giao dịch nào). Danh sách trả về sắp theo thứ tự hiển
/// thị (SortOrder rồi tên).
/// </summary>
public static class RevenueExpenseRule
{
    /// <summary>Chọn và sắp danh sách loại chi phí dùng cho một loại giao dịch.</summary>
    public static IReadOnlyList<RevenueExpenseType> Resolve(
        IEnumerable<RevenueExpenseType> expenseTypes,
        TransactionType transactionType)
    {
        var active = (expenseTypes ?? Enumerable.Empty<RevenueExpenseType>())
            .Where(t => !t.IsDeleted)
            .ToList();

        // Loại được gắn riêng cho đúng loại giao dịch này.
        var scoped = active
            .Where(t => t.Scopes.Any(s => !s.IsDeleted && s.TransactionType == transactionType))
            .ToList();

        // Không có loại gắn riêng thì lấy nhóm mặc định (không gắn loại giao dịch nào).
        var chosen = scoped.Count > 0
            ? scoped
            : active.Where(t => t.Scopes.All(s => s.IsDeleted)).ToList();

        return chosen
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
