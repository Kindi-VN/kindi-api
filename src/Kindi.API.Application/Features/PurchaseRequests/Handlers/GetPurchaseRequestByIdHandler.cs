using MediatR;
using AutoMapper;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.Features.PurchaseRequests.Queries;
using Kindi.API.Application.DTOs.responses;

namespace Kindi.API.Application.Features.PurchaseRequests.Handlers;

public class GetPurchaseRequestByIdHandler : IRequestHandler<GetPurchaseRequestByIdQuery, PurchaseRequestResponseDto>
{
	private readonly IRepository<PurchaseRequest> _repository;
	private readonly IMapper _mapper;
	private readonly IReferralService _referralService;

	public GetPurchaseRequestByIdHandler(
		IRepository<PurchaseRequest> repository,
		IMapper mapper,
		IReferralService referralService)
	{
		_repository = repository;
		_mapper = mapper;
		_referralService = referralService;
	}

	public async Task<PurchaseRequestResponseDto> Handle(GetPurchaseRequestByIdQuery request, CancellationToken cancellationToken)
	{
		// Thông tin cá nhân nằm ở bảng Users nên nạp kèm để map ra DTO
		var entity = await _repository.GetFirstWithIncludesAsync(
			x => x.Id == request.Id,
			includes: q => q.Include(x => x.User),
			cancellationToken: cancellationToken);
		if (entity == null || entity.IsDeleted)
			return null!;

		var dto = _mapper.Map<PurchaseRequestResponseDto>(entity);
		await _referralService.FillNamesAsync(new[] { dto }, x => x.ReferralCode, (x, name) => x.ReferralName = name);
		return dto;
	}
}