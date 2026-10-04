// GetOfferRequestsHandler.cs
using AutoMapper;
using Kindi.API.Application.Common.Extensions;
using Kindi.API.Application.Common.Helpers;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.Common.Mappings;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Application.Features.OfferRequests.Queries;
using Kindi.API.Domain.Entities;
using Kindi.API.Application.Resources;
using Kindi.API.Domain.Models;
using Kindi.API.Shared.Exceptions;
using Kindi.API.Shared.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Linq;
using Kindi.API.Domain.Enums;

namespace Kindi.API.Application.Features.OfferRequests.Handlers;

public class GetOfferRequestsHandler : IRequestHandler<GetOfferRequestsQuery, PagedList<OfferRequestResponseDto>>
{

	private readonly IQueryService _queryService;
	private readonly IMapper _mapper;
	private readonly IReferralService _referralService;
	private readonly ICurrentUserService _currentUserService;
	private readonly IStringLocalizer<SharedResource> _localizer;

	public GetOfferRequestsHandler(
		IQueryService queryService,
		IMapper mapper,
		IReferralService referralService,
		ICurrentUserService currentUserService,
		IStringLocalizer<SharedResource> localizer)
	{
		_queryService = queryService;
		_mapper = mapper;
		_referralService = referralService;
		_currentUserService = currentUserService;
		_localizer = localizer;
	}

	public async Task<PagedList<OfferRequestResponseDto>> Handle(GetOfferRequestsQuery request, CancellationToken cancellationToken)
	{
		var search = request.Search?.Trim();

		// Quyền xem: admin thấy tất cả (kể cả bản ghi đã xóa), người dùng thường chỉ thấy yêu cầu của chính mình.
		var isAdmin = _currentUserService.IsInRole(UserRole.Admin);
		var onlyMine = request.MineOnly || !isAdmin;
		var meId = GetCurrentUserId();

		if (onlyMine && meId == null)
			throw new UnauthorizedException(_localizer["UserNotAuthenticated"]);

		var mineId = meId ?? Guid.Empty;
		var includeDeleted = request.IncludeDeleted == true && isAdmin;

		// Thông tin cá nhân nằm ở bảng Users — kèm User để tìm kiếm và map DTO.
		IQueryable<OfferRequest> source = includeDeleted
			? _queryService.GetQueryableNoTracking<OfferRequest>().IgnoreQueryFilters().Where(x => x.IsDeleted).Include(x => x.User)
			: _queryService.GetAllNoTracking<OfferRequest>().Include(x => x.User);

		IQueryable<OfferRequest> q = source
			.WhereIf(onlyMine, x => x.UserId == mineId)
			.WhereIf(request.IsOfferSent.HasValue && !includeDeleted, x => x.IsOfferSent == request.IsOfferSent!.Value)
			.WhereIf(request.Status.HasValue && !includeDeleted, x => x.Status == request.Status!.Value)
			.WhereIf(request.FromDate.HasValue, x => x.CreatedAt >= request.FromDate!.Value.Date.ToUniversalTime())
			.WhereIf(request.ToDate.HasValue, x => x.CreatedAt < request.ToDate!.Value.Date.AddDays(1).ToUniversalTime());

		// searchField chỉ định thì chỉ dò đúng một trường; bỏ trống thì dò mọi trường như trước.
		q = RequestSearchFilters.ApplyOffer(q, search, request.SearchField);

		var pagedEntities = await q.ToPagedListAsync(
			request.Page,
			request.PageSize,
			request.SortBy,
			request.SortOrder,
			defaultSortBy: "CreatedAt",
			cancellationToken);

		var result = _mapper.MapPagedList<OfferRequest, OfferRequestResponseDto>(pagedEntities);
		await _referralService.FillNamesAsync(result.Items, x => x.RecordReferrerCode, (x, name) => x.RecordReferrerName = name);
		await _referralService.FillNamesAsync(result.Items, x => x.AccountReferrerCode, (x, name) => x.AccountReferrerName = name);
		return result;
	}

	private Guid? GetCurrentUserId()
		=> string.IsNullOrEmpty(_currentUserService.UserId)
			? null
			: Guid.Parse(_currentUserService.UserId!);
}