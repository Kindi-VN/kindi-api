using Kindi.API.Application.DTOs.responses;
using MediatR;

namespace Kindi.API.Application.Features.OfferRequests.Queries;

public class GetOfferRequestByIdQuery : IRequest<OfferRequestResponseDto?>
{
	public Guid Id { get; set; }
}