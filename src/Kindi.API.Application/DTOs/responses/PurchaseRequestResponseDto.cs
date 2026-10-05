using AutoMapper;
using Kindi.API.Application.Common.Helpers;
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

    /// <summary>Tên chủ thể của mã chia sẻ (CTV hoặc tài khoản) — hiển thị ở màn quản trị.</summary>
    public string? ReferralName { get; set; }

    /// <summary>Mã chia sẻ của người đã giới thiệu người tạo bản ghi (ghi nhận trên tài khoản).</summary>
    public string? ReferredByCode { get; set; }
    /// <summary>Tên CTV của <see cref="ReferredByCode"/>.</summary>
    public string? ReferredByName { get; set; }
    public PurchaseRequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Mã tài khoản (USR-…) của người gửi.</summary>
    public string? UserCode { get; set; }

    public void Mapping(Profile profile)
    {
        // Thông tin cá nhân nằm ở bảng Users — lấy qua navigation User khi map DTO.
        profile.CreateMap<PurchaseRequest, PurchaseRequestResponseDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : string.Empty))
            .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.User != null ? src.User.Phone : string.Empty))
            .ForMember(dest => dest.Zalo, opt => opt.MapFrom(src => src.User != null ? src.User.Zalo : null))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User != null ? UserInfo.DisplayEmail(src.User.Email, src.User.Phone) : null))
            .ForMember(dest => dest.ReferredByCode,
                opt => opt.MapFrom(src => src.User != null ? src.User.ReferredByCode : null))
            .ForMember(dest => dest.UserCode,
                opt => opt.MapFrom(src => src.User != null ? src.User.UserCode : null));
    }
}