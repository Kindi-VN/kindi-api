using AutoMapper;
using Kindi.API.Application.Common.Mappings;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;

namespace Kindi.API.Application.DTOs.responses;

public class GroupBuyingRequestResponseDto : IMapFrom<GroupBuyingRequest>
{
    public Guid Id { get; set; }
    public string? GroupBuyingRequestCode { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductLink { get; set; }
    public int TargetPeopleCount { get; set; }
    public int CurrentPeopleCount { get; set; }
    /// <summary>Số người còn thiếu để đạt mục tiêu (không âm).</summary>
    public int NeededPeopleCount { get; set; }
    public decimal? TargetPrice { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Zalo { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Note { get; set; }
    public GroupBuyingStatus Status { get; set; }
    public Guid? BusinessFieldId { get; set; }
    public string? BusinessFieldName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ClosedReason { get; set; }

    /// <summary>Mã CTV đã mang khách tới yêu cầu này (lấy từ link chia sẻ).</summary>
    public string? ReferralCode { get; set; }

    /// <summary>Tên chủ thể của mã chia sẻ (CTV hoặc tài khoản) — hiển thị ở màn quản trị.</summary>
    public string? ReferralName { get; set; }

    public void Mapping(Profile profile)
        => profile.CreateMap<GroupBuyingRequest, GroupBuyingRequestResponseDto>()
            .ForMember(dest => dest.NeededPeopleCount,
                opt => opt.MapFrom(src => src.TargetPeopleCount > src.CurrentPeopleCount
                    ? src.TargetPeopleCount - src.CurrentPeopleCount
                    : 0))
            .ForMember(dest => dest.BusinessFieldName,
                opt => opt.MapFrom(src => src.BusinessField != null ? src.BusinessField.Name : null));
}
