using AutoMapper;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Application.Features.OfferRequests.Queries;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace Kindi.API.Application.Features.OfferRequests.Handlers;

public class GetOfferRequestByIdHandler : IRequestHandler<GetOfferRequestByIdQuery, OfferRequestResponseDto?>
{
	private readonly IRepository<OfferRequest> _repository;
	private readonly IMapper _mapper;
	private readonly IReferralService _referralService;

	public GetOfferRequestByIdHandler(
		IRepository<OfferRequest> repository,
		IMapper mapper,
		IReferralService referralService)
	{
		_repository = repository;
		_mapper = mapper;
		_referralService = referralService;
	}

	public async Task<OfferRequestResponseDto?> Handle(GetOfferRequestByIdQuery request, CancellationToken cancellationToken)
	{
		// Thông tin cá nhân nằm ở bảng Users nên nạp kèm để map ra DTO
		var entity = await _repository.GetFirstWithIncludesAsync(
			x => x.Id == request.Id,
			includes: q => q.Include(x => x.User),
			cancellationToken: cancellationToken);
		if (entity == null || entity.IsDeleted)
			return null;

		var dto = _mapper.Map<OfferRequestResponseDto>(entity);
		await _referralService.FillNamesAsync(new[] { dto }, x => x.ReferralCode, (x, name) => x.ReferralName = name);
		return dto;
	}
}