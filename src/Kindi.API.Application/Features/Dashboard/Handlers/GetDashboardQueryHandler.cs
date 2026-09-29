using Kindi.API.Application.DTOs.Responses;
using Kindi.API.Application.Features.Dashboard.Queries;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Kindi.API.Application.Features.Dashboard.Handlers;

public class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, DashboardStatsDto>
{
	private readonly IRepository<PurchaseRequest> _purchaseRepo;
	private readonly IRepository<GroupBuyingRequest> _groupBuyingRepo;
	private readonly IRepository<OfferRequest> _offerRepo;
	private readonly IRepository<Collaborator> _ctvRepo;
	private readonly IRepository<User> _userRepo;
	private readonly ILogger<GetDashboardQueryHandler> _logger;

	public GetDashboardQueryHandler(
		IRepository<PurchaseRequest> purchaseRepo,
		IRepository<GroupBuyingRequest> groupBuyingRepo,
		IRepository<OfferRequest> offerRepo,
		IRepository<Collaborator> ctvRepo,
		IRepository<User> userRepo,
		ILogger<GetDashboardQueryHandler> logger)
	{
		_purchaseRepo = purchaseRepo;
		_groupBuyingRepo = groupBuyingRepo;
		_offerRepo = offerRepo;
		_ctvRepo = ctvRepo;
		_userRepo = userRepo;
		_logger = logger;
	}

	public async Task<DashboardStatsDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Admin dashboard stats requested");

		var stats = new DashboardStatsDto
		{
			TotalPurchaseRequests = await CountAsync(_purchaseRepo, x => !x.IsDeleted, cancellationToken),
			PendingPurchaseRequests = await CountAsync(_purchaseRepo, x => !x.IsDeleted && x.Status == PurchaseRequestStatus.Pending, cancellationToken),
			TotalGroupBuyingRequests = await CountAsync(_groupBuyingRepo, x => !x.IsDeleted, cancellationToken),
			PendingGroupBuyingRequests = await CountAsync(_groupBuyingRepo, x => !x.IsDeleted && x.Status == GroupBuyingStatus.Pending, cancellationToken),
			TotalOfferRequests = await CountAsync(_offerRepo, x => !x.IsDeleted, cancellationToken),
			PendingOfferRequests = await CountAsync(_offerRepo, x => !x.IsDeleted && x.Status == OfferStatus.Pending, cancellationToken),
			TotalCTVRegistrations = await CountAsync(_ctvRepo, x => !x.IsDeleted, cancellationToken),
			PendingCTVRegistrations = await CountAsync(_ctvRepo, x => !x.IsDeleted && x.Status == CollaboratorStatus.Pending, cancellationToken),
			RecentActivities = await GetRecentActivities(cancellationToken)
		};

		return stats;
	}

	private async Task<int> CountAsync<T>(IRepository<T> repo, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken) where T : BaseEntity
	{
		var items = await repo.FindAsync(predicate, cancellationToken);
		return items.Count();
	}

	private async Task<List<RecentActivityDto>> GetRecentActivities(CancellationToken cancellationToken)
	{
		var activities = new List<RecentActivityDto>();

		var users = await _userRepo.FindAsync(x => !x.IsDeleted, cancellationToken);
		var userDict = users.ToDictionary(u => u.Id, u => u.FullName);

		var purchases = await _purchaseRepo.FindAsync(x => !x.IsDeleted, cancellationToken);
		activities.AddRange(purchases.Select(x => new RecentActivityDto
		{
			Type = "purchase_request",
			Action = "created",
			UserName = userDict.GetValueOrDefault(x.UserId) ?? string.Empty,
			Timestamp = x.CreatedAt
		}));

		var groupBuyings = await _groupBuyingRepo.FindAsync(x => !x.IsDeleted, cancellationToken);
		activities.AddRange(groupBuyings.Select(x => new RecentActivityDto
		{
			Type = "group_buying_request",
			Action = "created",
			UserName = userDict.GetValueOrDefault(x.UserId) ?? string.Empty,
			Timestamp = x.CreatedAt
		}));

		var offers = await _offerRepo.FindAsync(x => !x.IsDeleted, cancellationToken);
		activities.AddRange(offers.Select(x => new RecentActivityDto
		{
			Type = "offer_request",
			Action = "created",
			UserName = userDict.GetValueOrDefault(x.UserId) ?? string.Empty,
			Timestamp = x.CreatedAt
		}));

		var ctvs = await _ctvRepo.FindAsync(x => !x.IsDeleted, cancellationToken);
		activities.AddRange(ctvs.Select(x => new RecentActivityDto
		{
			Type = "ctv_registration",
			Action = "registered",
			UserName = userDict.GetValueOrDefault(x.UserId) ?? string.Empty,
			Timestamp = x.CreatedAt
		}));

		return activities.OrderByDescending(x => x.Timestamp).Take(10).ToList();
	}
}