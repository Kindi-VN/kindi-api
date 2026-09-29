using Kindi.API.Application.Common.Helpers;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.Features.PurchaseRequests.Queries;
using Kindi.API.Application.Resources;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Shared.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Linq.Expressions;

namespace Kindi.API.Application.Features.PurchaseRequests.Handlers;

public class ExportPurchaseRequestsHandler : IRequestHandler<ExportPurchaseRequestsQuery, byte[]>
{
	private readonly IRepository<PurchaseRequest> _repository;
	private readonly IExcelService _excelService;
	private readonly IStringLocalizer<SharedResource> _localizer;

	public ExportPurchaseRequestsHandler(
		IRepository<PurchaseRequest> repository,
		IExcelService excelService,
		IStringLocalizer<SharedResource> localizer)
	{
		_repository = repository;
		_excelService = excelService;
		_localizer = localizer;
	}

	public async Task<byte[]> Handle(ExportPurchaseRequestsQuery request, CancellationToken cancellationToken)
	{
		var predicate = BuildPredicate(request);
		// Thông tin cá nhân nằm ở bảng Users — kèm User để xuất đúng cột họ tên/SĐT/email.
		var entities = await _repository.GetListWithIncludesAsync(
			includes: q => q.Include(x => x.User),
			predicate: predicate,
			cancellationToken: cancellationToken);
		var data = entities.OrderByDescending(x => x.CreatedAt).ToList();

		var columns = new Dictionary<string, Func<PurchaseRequest, object>>
		{
			["ExportPurchaseRequests_STT"] = x => data.IndexOf(x) + 1,
			["ExportPurchaseRequests_ProductName"] = x => x.ProductName,
			["ExportPurchaseRequests_Quantity"] = x => x.Quantity,
			["ExportPurchaseRequests_ExpectedPrice"] = x => x.ExpectedPrice.GetValueOrDefault(),
			["ExportPurchaseRequests_FullName"] = x => x.User != null ? x.User.FullName : string.Empty,
			["ExportPurchaseRequests_Phone"] = x => x.User != null ? (x.User.Phone ?? string.Empty) : string.Empty,
			["ExportPurchaseRequests_Email"] = x => x.User != null ? (UserInfo.DisplayEmail(x.User.Email, x.User.Phone) ?? string.Empty) : string.Empty,
			["ExportPurchaseRequests_Status"] = x => x.Status.ToString(),
			["ExportPurchaseRequests_CreatedAt"] = x => x.CreatedAt
		};

		return _excelService.ExportToExcel(
			data,
			columns,
			"PurchaseRequests",
			"ExportPurchaseRequestsTitle",
			_localizer);
	}

	private static Expression<Func<PurchaseRequest, bool>> BuildPredicate(ExportPurchaseRequestsQuery request)
	{
		Expression<Func<PurchaseRequest, bool>> predicate = x => !x.IsDeleted;

		if (request.Status.HasValue)
		{
			var status = request.Status.Value;
			predicate = predicate.AndAlso(x => x.Status == status);
		}

		if (request.FromDate.HasValue)
		{
			var from = request.FromDate.Value.Date;
			predicate = predicate.AndAlso(x => x.CreatedAt >= from);
		}

		if (request.ToDate.HasValue)
		{
			var to = request.ToDate.Value.Date.AddDays(1);
			predicate = predicate.AndAlso(x => x.CreatedAt < to);
		}

		return predicate;
	}
}