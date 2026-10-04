namespace Kindi.API.Application.Common.Interfaces;

using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Models;

/// <summary>
/// Khai doanh thu của một giao dịch: quản trị viên nhập doanh thu gộp, hệ thống trừ thuế, trừ hoa hồng
/// từng bên và chi phí phát sinh để ra số thực nhận; chốt xong thì khoá lại.
/// </summary>
public interface ITransactionRevenueService
{
    /// <summary>Danh sách bản khai doanh thu, có lọc theo mã giao dịch, loại và trạng thái.</summary>
    Task<PagedList<TransactionRevenueResponse>> GetPagedAsync(RevenueQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>Bản khai của một giao dịch (null nếu giao dịch chưa được khai).</summary>
    Task<TransactionRevenueResponse?> GetByReferenceAsync(TransactionType type, Guid referenceId, CancellationToken cancellationToken = default);

    /// <summary>Một bản khai theo id (null nếu không có).</summary>
    Task<TransactionRevenueResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Lưu nháp bản khai: tính lại toàn bộ số liệu rồi ghi đè. Bản đã chốt thì không sửa được.</summary>
    Task<TransactionRevenueResponse> SaveAsync(SaveTransactionRevenueRequest request, CancellationToken cancellationToken = default);

    /// <summary>Chốt số liệu: tính lại lần cuối, ghi người chốt và khoá bản khai.</summary>
    Task<TransactionRevenueResponse> ConfirmAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Thống kê doanh thu theo khoảng thời gian, chỉ tính bản khai đã chốt.</summary>
    Task<RevenueStatsResponse> GetStatsAsync(RevenueStatsQueryDto query, CancellationToken cancellationToken = default);
}
