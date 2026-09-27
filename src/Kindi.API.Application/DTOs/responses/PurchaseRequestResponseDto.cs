using AutoMapper;
using Kindi.API.Application.Common.Mappings;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;

namespace Kindi.API.Application.DTOs.responses;

public class PurchaseRequestResponseDto : IMapFrom<PurchaseRequest>
{
    public Guid Id { get; set; }
    public string? PurchaseRequestCode { get; set; }
    public Guid UserId { get; set; }
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

    /// <summary>Mã CTV đã mang khách tới yêu cầu này (lấy từ link chia sẻ).</summary>
    public string? ReferralCode { get; set; }
    public PurchaseRequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<PurchaseRequest, PurchaseRequestResponseDto>();
    }
}