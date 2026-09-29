// GetOfferRequestsHandler.cs
using AutoMapper;
using Kindi.API.Application.Common.Extensions;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.Common.Mappings;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Application.Features.OfferRequests.Queries;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Kindi.API.Application.Features.OfferRequests.Handlers;

public class GetOfferRequestsHandler : IRequestHandler<GetOfferRequestsQuery, PagedList<OfferRequestResponseDto>>
{
	private readonly IQueryService _queryService;
	private readonly IMapper _mapper;
	private readonly IReferralService _referralService;

	public GetOfferRequestsHandler(IQueryService queryService, IMapper mapper, IReferralService referralService)
	{
		_queryService = queryService;
		_mapper = mapper;
		_referralService = referralService;
	}

	public async Task<PagedList<OfferRequestResponseDto>> Handle(GetOfferRequestsQuery request, CancellationToken cancellationToken)
	{
		var search = request.Search?.Trim();
		var includeDeleted = request.IncludeDeleted == true;

		// Thông tin cá nhân nằm ở bảng Users — kèm User để tìm kiếm và map DTO.
		IQueryable<OfferRequest> source = includeDeleted
			? _queryService.GetQueryableNoTracking<OfferRequest>().IgnoreQueryFilters().Where(x => x.IsDeleted).Include(x => x.User)
			: _queryService.GetAllNoTracking<OfferRequest>().Include(x => x.User);

		var q = source
			.WhereIf(request.IsOfferSent.HasValue && !includeDeleted, x => x.IsOfferSent == request.IsOfferSent!.Value)
			.WhereIf(request.Status.HasValue && !includeDeleted, x => x.Status == request.Status!.Value)
			.WhereIf(!string.IsNullOrEmpty(search), x =>
				x.ProductName.Contains(search!) ||
				(x.User != null && x.User.FullName.Contains(search!)) ||
				(x.User != null && x.User.Phone != null && x.User.Phone.Contains(search!)) ||
				(x.User != null && x.User.Email != null && x.User.Email.Contains(search!)) ||
				(x.OfferRequestCode != null && x.OfferRequestCode.Contains(search!)))
			.WhereIf(request.FromDate.HasValue, x => x.CreatedAt >= request.FromDate!.Value.Date.ToUniversalTime())
			.WhereIf(request.ToDate.HasValue, x => x.CreatedAt < request.ToDate!.Value.Date.AddDays(1).ToUniversalTime());

		var pagedEntities = await q.ToPagedListAsync(
			request.Page,
			request.PageSize,
			request.SortBy,
			request.SortOrder,
			defaultSortBy: "CreatedAt",
			cancellationToken);

		var result = _mapper.MapPagedList<OfferRequest, OfferRequestResponseDto>(pagedEntities);
		await _referralService.FillNamesAsync(result.Items, x => x.ReferralCode, (x, name) => x.ReferralName = name);
		return result;
	}
}