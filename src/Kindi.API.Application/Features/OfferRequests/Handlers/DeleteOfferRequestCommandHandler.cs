using AutoMapper;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Application.Features.OfferRequests.Commands;
using Kindi.API.Application.Resources;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Shared.Exceptions;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Kindi.API.Application.Features.OfferRequests.Handlers;

public class DeleteOfferRequestCommandHandler : IRequestHandler<DeleteOfferRequestCommand, OfferRequestResponseDto>
{
	private readonly IRepository<OfferRequest> _repository;
	private readonly IMapper _mapper;
	private readonly IStringLocalizer<SharedResource> _localizer;

	public DeleteOfferRequestCommandHandler(
		IRepository<OfferRequest> repository,
		IMapper mapper,
		IStringLocalizer<SharedResource> localizer)
	{
		_repository = repository;
		_mapper = mapper;
		_localizer = localizer;
	}

	public async Task<OfferRequestResponseDto> Handle(DeleteOfferRequestCommand request, CancellationToken cancellationToken)
	{
		var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
		if (entity == null || entity.IsDeleted)
			throw new NotFoundException(_localizer["OfferRequestNotFound"]);

		_repository.Delete(entity);
		await _repository.SaveChangesAsync(cancellationToken);

		return _mapper.Map<OfferRequestResponseDto>(entity);
	}
}