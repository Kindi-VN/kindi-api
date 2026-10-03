using Kindi.API.Application.DTOs.Requests;
using Kindi.API.Application.DTOs.Responses;
using Kindi.API.Domain.Models;

namespace Kindi.API.Application.Common.Interfaces;

public interface IPartnerService
{
    Task<PartnerRegisterResponse> RegisterAsync(PartnerRegisterRequest request);

    /// <summary>
    /// Danh sách đối tác doanh nghiệp đã duyệt/đang hoạt động (trang Nguồn cung công khai).
    /// </summary>
    Task<PagedList<PublicPartnerResponseDto>> GetPublicPagedAsync(PublicPartnerQueryDto query);
    Task<PagedList<PartnerResponseDto>> GetPagedAsync(PartnerFilterRequest filter);
    Task<PartnerDetailResponseDto?> GetDetailAsync(Guid id);
    Task<PartnerResponseDto> ApproveAsync(Guid id);
    Task<PartnerResponseDto> RejectAsync(Guid id);
    Task<PartnerResponseDto> ActivateAsync(Guid id);
    /// <summary>Cập nhật thông tin đối tác (partial update — field null giữ nguyên).</summary>
    Task<PartnerResponseDto> UpdateAsync(Guid id, UpdatePartnerDto request);

    // ===== Sản phẩm / dịch vụ của đối tác =====
    Task<PartnerProductDto> AddProductAsync(Guid partnerId, CreatePartnerProductDto request);
    Task<PartnerProductDto> UpdateProductAsync(Guid partnerId, Guid productId, UpdatePartnerProductDto request);
    Task DeleteProductAsync(Guid partnerId, Guid productId);

    /// <summary>Xóa mềm đối tác.</summary>
    Task DeleteAsync(Guid id);
    /// <summary>Khôi phục đối tác đã xóa mềm.</summary>
    Task<PartnerResponseDto> RestoreAsync(Guid id);
    /// <summary>Danh sách đối tác đã xóa mềm (bỏ qua global query filter).</summary>
    Task<PagedList<PartnerResponseDto>> GetPagedDeletedAsync(int pageNumber, int pageSize, string? search = null);
}