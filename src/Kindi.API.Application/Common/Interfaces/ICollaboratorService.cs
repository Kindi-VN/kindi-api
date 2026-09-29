using Kindi.API.Application.DTOs.Requests;
using Kindi.API.Application.DTOs.Responses;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Models;

namespace Kindi.API.Application.Common.Interfaces;

public interface ICollaboratorService
{
    Task<CollaboratorResponseDto> CreateAsync(CreateCollaboratorDto request);

    /// <summary>
    /// Tạo hồ sơ cộng tác viên (chờ duyệt) cho người dùng nếu chưa có — nơi DUY NHẤT sinh bản ghi
    /// ở bảng <c>Collaborators</c> cho các luồng công khai (mua chung, nhóm ngành).
    /// </summary>
    Task EnsureProfileForUserAsync(Guid userId);
    Task<CollaboratorResponseDto> UpdateAsync(Guid id, UpdateCollaboratorDto request);
    Task<CollaboratorResponseDto> GetByIdAsync(Guid id);
    /// <summary>
    /// Danh sách cộng tác viên phân trang (Admin). Lọc theo trạng thái + khoảng ngày tạo.
    /// </summary>
    Task<PagedList<CollaboratorResponseDto>> GetPagedAsync(
        int page,
        int size,
        string? search = null,
        CollaboratorStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// Danh sách cộng tác viên đã xóa mềm (Admin).
    /// </summary>
    Task<PagedList<CollaboratorResponseDto>> GetPagedDeletedAsync(int page, int size, string? search = null);
    Task ApproveAsync(Guid id);
    Task RejectAsync(Guid id, string? reason = null);
    Task DeleteAsync(Guid id);
    Task RestoreAsync(Guid id);
}