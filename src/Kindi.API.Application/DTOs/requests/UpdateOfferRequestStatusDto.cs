using Kindi.API.Domain.Enums;

namespace Kindi.API.Application.DTOs.requests;

public class UpdateOfferRequestStatusDto
{
	public OfferStatus Status { get; set; }
}