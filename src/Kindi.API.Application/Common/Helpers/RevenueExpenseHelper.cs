namespace Kindi.API.Application.Common.Helpers;

using Kindi.API.Application.DTOs.requests;

/// <summary>
/// Tiện ích tính chi phí phát sinh của bản khai doanh thu. Chi phí phát sinh bằng tổng các dòng chi phí
/// quản trị viên nhập; tách riêng ở đây để phép cộng tiền có test chốt.
/// </summary>
public static class RevenueExpenseHelper
{
    /// <summary>Tổng tiền các dòng chi phí; danh sách rỗng hoặc null trả về 0.</summary>
    public static decimal Total(IEnumerable<TransactionExpenseRequest>? lines)
        => (lines ?? Enumerable.Empty<TransactionExpenseRequest>()).Sum(e => e.Amount);
}
