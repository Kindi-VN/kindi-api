// src/Kindi.API.Application/DTOs/requests/CreateOfferRequestDto.cs
using AutoMapper;
using Kindi.API.Application.Common.Mappings;
using Kindi.API.Domain.Entities;

namespace Kindi.API.Application.DTOs.requests;

public class CreateOfferRequestDto : IMapFrom<OfferRequest>
{
    // Product information
    public string ProductName { get; set; } = string.Empty;
    public string? ProductLink { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal? ExpectedPrice { get; set; }
    public int Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;

    // User information
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Zalo { get; set; }
    public string? Email { get; set; }
    public string? Note { get; set; }

    /// <summary>Mã CTV của link chia sẻ khách dùng để tạo yêu cầu (không bắt buộc).</summary>
    public string? ReferralCode { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CreateOfferRequestDto, OfferRequest>();
    }
}