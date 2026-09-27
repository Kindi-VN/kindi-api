// src/Kindi.API.Application/DTOs/requests/CreateGroupBuyingRequestDto.cs
using AutoMapper;
using Kindi.API.Application.Common.Mappings;
using Kindi.API.Domain.Entities;

namespace Kindi.API.Application.DTOs.requests;

public class CreateGroupBuyingRequestDto : IMapFrom<GroupBuyingRequest>
{
    public string ProductName { get; set; } = string.Empty;
    public string? ProductLink { get; set; }              
    public int TargetPeopleCount { get; set; }
    public decimal? TargetPrice { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Zalo { get; set; }                     
    public string Email { get; set; } = string.Empty;     
    public string? Note { get; set; }

    /// <summary>Mã CTV của link chia sẻ khách dùng để tạo yêu cầu (không bắt buộc).</summary>
    public string? ReferralCode { get; set; }

    public void Mapping(Profile profile)
        => profile.CreateMap<CreateGroupBuyingRequestDto, GroupBuyingRequest>();
}