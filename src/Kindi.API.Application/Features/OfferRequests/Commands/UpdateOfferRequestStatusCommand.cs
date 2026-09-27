using Kindi.API.Application.DTOs.responses;
using Kindi.API.Domain.Enums;
using MediatR;

namespace Kindi.API.Application.Features.OfferRequests.Commands;

public class UpdateOfferRequestStatusCommand : IRequest<OfferRequestStatusResponseDto>
{
	public Guid Id { get; set; }
	public OfferStatus Status { get; set; }
}