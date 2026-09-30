// OfferRequestQueryDto.cs
using AutoMapper;
using Kindi.API.Application.Common.Mappings;
using Kindi.API.Application.Features.OfferRequests.Queries;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;

namespace Kindi.API.Application.DTOs.requests;

public class OfferRequestQueryDto : IMapFrom<GetOfferRequestsQuery>
{
	public int Page { get; set; } = 1;
	public int PageSize { get; set; } = 20;
	public bool? IsOfferSent { get; set; }
	public OfferStatus? Status { get; set; }
	public string? Search { get; set; }
	public string? SortBy { get; set; }
	public string? SortOrder { get; set; }
	public bool? IncludeDeleted { get; set; }
	public DateTime? FromDate { get; set; }
	public DateTime? ToDate { get; set; }

	/// <summary>
	/// Chỉ lấy yêu cầu của chính người gọi (khu vực thành viên). Người dùng không phải admin luôn bị giới hạn như vậy.
	/// </summary>
	public bool MineOnly { get; set; }

	public void Mapping(Profile profile)
	{
		profile.CreateMap<OfferRequestQueryDto, GetOfferRequestsQuery>();
	}
}