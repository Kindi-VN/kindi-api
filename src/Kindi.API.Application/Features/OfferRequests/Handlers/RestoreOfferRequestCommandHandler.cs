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

public class RestoreOfferRequestCommandHandler : IRequestHandler<RestoreOfferRequestCommand, OfferRequestResponseDto>
{
	private readonly IRepository<OfferRequest> _repository;
	private readonly IMapper _mapper;
	private readonly IStringLocalizer<SharedResource> _localizer;

	public RestoreOfferRequestCommandHandler(
		IRepository<OfferRequest> repository,
		IMapper mapper,
		IStringLocalizer<SharedResource> localizer)
	{
		_repository = repository;
		_mapper = mapper;
		_localizer = localizer;
	}

	public async Task<OfferRequestResponseDto> Handle(RestoreOfferRequestCommand request, CancellationToken cancellationToken)
	{
		// GetByIdIncludingDeletedAsync bỏ qua global soft-delete filter → lấy được record đã xóa mềm.
		var entity = await _repository.GetByIdIncludingDeletedAsync(request.Id, cancellationToken);
		if (entity == null || !entity.IsDeleted)
			throw new NotFoundException(_localizer["OfferRequestNotFound"]);

		entity.IsDeleted = false;
		entity.UpdatedAt = DateTime.UtcNow;

		_repository.Update(entity);
		await _repository.SaveChangesAsync(cancellationToken);

		return _mapper.Map<OfferRequestResponseDto>(entity);
	}
}