namespace Kindi.API.Application.Common.Interfaces;

using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;

/// <summary>Hạng thành viên: cấu hình quyền lợi của hạng và xét hạng theo doanh số tích luỹ.</summary>
public interface IMembershipTierService
{
    /// <summary>Danh sách hạng thành viên, xếp theo thứ tự hạng.</summary>
    Task<List<MembershipTierResponse>> GetTiersAsync(bool activeOnly = false, CancellationToken cancellationToken = default);

    /// <summary>Thêm mới hoặc cập nhật một hạng thành viên.</summary>
    Task<MembershipTierResponse> SaveAsync(Guid? id, SaveMembershipTierRequest request, CancellationToken cancellationToken = default);

    /// <summary>Xoá một hạng thành viên.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Danh sách hạng thành viên đã xoá mềm (bỏ qua global soft-delete filter).</summary>
    Task<List<MembershipTierResponse>> GetDeletedAsync(CancellationToken cancellationToken = default);

    /// <summary>Khôi phục một hạng thành viên đã xoá mềm.</summary>
    Task<MembershipTierResponse> RestoreAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Xét lại hạng thành viên theo doanh số tích luỹ; trả về số tài khoản đã xét.</summary>
    Task<int> EvaluateAsync(EvaluateMembershipRequest request, CancellationToken cancellationToken = default);

    /// <summary>Hạng thành viên và quyền lợi hiện tại của chính người gọi.</summary>
    Task<MyMembershipResponse> GetMineAsync(CancellationToken cancellationToken = default);
}
