using AutoMapper;
using Kindi.API.Application.Common.Mappings;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using MediatR;

namespace Kindi.API.Application.Features.PurchaseRequests.Commands;

public class CreatePurchaseRequestCommand : IRequest<PurchaseRequestResponseDto>, IMapFrom<CreatePurchaseRequestDto>
{
    public string ProductName { get; set; } = string.Empty;
    public string? ProductCategory { get; set; }
    public int Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal? ExpectedPrice { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Zalo { get; set; }
    public string? Email { get; set; }
    public string? Note { get; set; }

    /// <summary>Mã CTV của link chia sẻ khách dùng để tạo yêu cầu (không bắt buộc).</summary>
    public string? ReferralCode { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CreatePurchaseRequestDto, CreatePurchaseRequestCommand>();
    }
}