using Kindi.API.Application.DTOs.responses;
using MediatR;

namespace Kindi.API.Application.Features.OfferRequests.Commands;

public class DeleteOfferRequestCommand : IRequest<OfferRequestResponseDto>
{
	public Guid Id { get; set; }
}