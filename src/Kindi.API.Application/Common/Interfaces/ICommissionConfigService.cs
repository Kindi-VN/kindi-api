using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Domain.Models;

namespace Kindi.API.Application.Common.Interfaces;

/// <summary>
/// Cấu hình mức hoa hồng: bản chung cho mọi tài khoản và bản riêng cho từng tài khoản
/// (chọn một hoặc nhiều tài khoản cùng lúc). Bản riêng luôn ưu tiên hơn bản chung.
/// </summary>
public interface ICommissionConfigService
{
    /// <summary>Danh sách cấu hình hoa hồng (bản chung và bản riêng).</summary>
    Task<PagedList<CommissionConfigResponse>> GetPagedAsync(CommissionConfigQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lưu cấu hình cho bản chung hoặc cho từng tài khoản trong yêu cầu; tài khoản đã có cấu hình thì cập nhật lại.
    /// </summary>
    Task<List<CommissionConfigResponse>> SaveAsync(SaveCommissionConfigRequest request, CancellationToken cancellationToken = default);

    /// <summary>Xoá một cấu hình hoa hồng.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Mức hoa hồng đang áp cho chính người gọi, theo từng bên nhận.</summary>
    Task<MyCommissionResponse> GetMineAsync(CancellationToken cancellationToken = default);

    /// <summary>Mức hoa hồng đang áp cho một tài khoản (bản riêng trước, không có thì bản chung).</summary>
    Task<CommissionConfigResponse?> GetEffectiveAsync(Domain.Enums.CommissionBeneficiary beneficiary, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Tìm tài khoản để cấu hình hoa hồng riêng (theo tên đăng nhập, họ tên hoặc số điện thoại).</summary>
    Task<List<CommissionUserResponse>> SearchUsersAsync(string? search, CancellationToken cancellationToken = default);
}
