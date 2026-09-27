using Kindi.API.Application.DTOs.responses;
using MediatR;

namespace Kindi.API.Application.Features.OfferRequests.Commands;

public class RestoreOfferRequestCommand : IRequest<OfferRequestResponseDto>
{
	public Guid Id { get; set; }
}