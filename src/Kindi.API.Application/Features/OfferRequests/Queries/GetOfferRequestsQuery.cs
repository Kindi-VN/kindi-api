// GetOfferRequestsQuery.cs
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Models;
using MediatR;

namespace Kindi.API.Application.Features.OfferRequests.Queries;

public class GetOfferRequestsQuery : IRequest<PagedList<OfferRequestResponseDto>>
{
	public int Page { get; set; } = 1;
	public int PageSize { get; set; } = 20;
	public bool? IsOfferSent { get; set; }
	public OfferStatus? Status { get; set; }
	public string? Search { get; set; }
	public string? SortBy { get; set; }
	public string? SortOrder { get; set; }

	/// <summary>Lọc danh sách đã xóa (soft-delete, admin xem tab "Đã xóa").</summary>
	public bool? IncludeDeleted { get; set; }

	/// <summary>Lọc theo khoảng ngày tạo.</summary>
	public DateTime? FromDate { get; set; }
	public DateTime? ToDate { get; set; }

	/// <summary>
	/// Chỉ lấy yêu cầu của chính người gọi (khu vực thành viên). Người dùng không phải admin luôn bị giới hạn như vậy.
	/// </summary>
	public bool MineOnly { get; set; }
}