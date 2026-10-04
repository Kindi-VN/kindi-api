using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Domain.Models;

namespace Kindi.API.Application.Common.Interfaces;

public interface IPurchaseRequestService
{
	Task<PurchaseRequestResponseDto> CreateAsync(CreatePurchaseRequestDto request);
	Task<PagedList<PurchaseRequestResponseDto>> GetPagedAsync(PurchaseRequestQueryDto query);
	Task<PurchaseRequestStatusResponseDto> UpdateStatusAsync(Guid id, UpdatePurchaseRequestStatusDto dto);

	/// <summary>Admin xoá mềm một yêu cầu mua hàng.</summary>
	Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

	/// <summary>Admin khôi phục một yêu cầu mua hàng đã xoá mềm.</summary>
	Task<PurchaseRequestResponseDto> RestoreAsync(Guid id, CancellationToken cancellationToken = default);
}