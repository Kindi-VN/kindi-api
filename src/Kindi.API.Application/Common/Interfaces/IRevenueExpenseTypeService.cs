namespace Kindi.API.Application.Common.Interfaces;

using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Domain.Enums;

/// <summary>
/// Cấu hình loại chi phí dùng chung khi khai doanh thu: mỗi loại có thể gắn cho một số loại giao dịch
/// nhất định, loại không gắn gì là loại mặc định. Khi khai doanh thu, hệ thống giải ra đúng nhóm loại
/// chi phí áp cho loại giao dịch đang khai.
/// </summary>
public interface IRevenueExpenseTypeService
{
    /// <summary>Toàn bộ loại chi phí đang dùng, kèm các loại giao dịch được gắn.</summary>
    Task<List<RevenueExpenseTypeResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Tạo một loại chi phí mới kèm scope.</summary>
    Task<RevenueExpenseTypeResponse> CreateAsync(SaveRevenueExpenseTypeRequest request, CancellationToken cancellationToken = default);

    /// <summary>Cập nhật tên, thứ tự và thay toàn bộ scope của một loại chi phí.</summary>
    Task<RevenueExpenseTypeResponse> UpdateAsync(Guid id, SaveRevenueExpenseTypeRequest request, CancellationToken cancellationToken = default);

    /// <summary>Xoá một loại chi phí cùng các scope của nó.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Đặt lại scope của một nhóm loại chi phí đúng bằng danh sách loại giao dịch truyền vào.</summary>
    Task AssignAsync(AssignRevenueExpenseTypeScopesRequest request, CancellationToken cancellationToken = default);

    /// <summary>Giải nhóm loại chi phí áp cho một loại giao dịch (dùng trong màn khai doanh thu).</summary>
    Task<List<RevenueExpenseTypeResolveResponse>> ResolveAsync(TransactionType type, CancellationToken cancellationToken = default);

    /// <summary>Đọc hai cột cấu hình thuế doanh thu trong bảng cài đặt chung.</summary>
    Task<RevenueExpenseConfigResponse> GetConfigAsync(CancellationToken cancellationToken = default);

    /// <summary>Cập nhật riêng hai cột cấu hình thuế doanh thu, giữ nguyên mọi cột khác.</summary>
    Task<RevenueExpenseConfigResponse> SaveConfigAsync(RevenueExpenseConfigRequest request, CancellationToken cancellationToken = default);
}
