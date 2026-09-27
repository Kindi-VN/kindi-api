using AutoMapper;
using Kindi.API.Application.Common.Mappings;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;

namespace Kindi.API.Application.DTOs.responses;

public class OfferRequestStatusResponseDto : IMapFrom<OfferRequest>
{
	public Guid Id { get; set; }
	public string? OfferRequestCode { get; set; }
	public OfferStatus Status { get; set; }
	public DateTime UpdatedAt { get; set; }

	public void Mapping(Profile profile)
		=> profile.CreateMap<OfferRequest, OfferRequestStatusResponseDto>();
}