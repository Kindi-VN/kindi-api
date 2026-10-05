using Kindi.API.Application.Common.Helpers;
using Kindi.API.Application.Common.Mappings;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using AutoMapper;

namespace Kindi.API.Application.DTOs.Responses;

public class PartnerResponseDto : IMapFrom<Partner>
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string PartnerCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyTax { get; set; } = string.Empty;
    public string CompanyAddress { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public BusinessType BusinessType { get; set; }
    public CompanySize CompanySize { get; set; }
    public string? CompanyWebsite { get; set; }
    /// <summary>Id lĩnh vực kinh doanh (BusinessField).</summary>
    public Guid? BusinessFieldId { get; set; }
    /// <summary>Tên lĩnh vực kinh doanh — lấy từ bảng BusinessFields.</summary>
    public string? BusinessFieldName { get; set; }
    public string? ReferralCode { get; set; }

    /// <summary>Mã chia sẻ của người đã giới thiệu người tạo bản ghi (ghi nhận trên tài khoản).</summary>
    public string? AccountReferrerCode { get; set; }
    /// <summary>Tên CTV của <see cref="AccountReferrerCode"/>.</summary>
    public string? AccountReferrerName { get; set; }
    public string? Note { get; set; }
    public PartnerStatus Status { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Partner, PartnerResponseDto>()
            // Thông tin cá nhân chỉ nằm ở bảng Users → lấy qua nav User (bắt buộc Include khi query).
            .ForMember(dest => dest.FullName,
                opt => opt.MapFrom(src => src.User != null ? src.User.FullName : string.Empty))
            .ForMember(dest => dest.Phone,
                opt => opt.MapFrom(src => src.User != null ? (src.User.Phone ?? string.Empty) : string.Empty))
            .ForMember(dest => dest.Email,
                opt => opt.MapFrom(src => (src.User != null ? UserInfo.DisplayEmail(src.User.Email, src.User.Phone) : null) ?? string.Empty))
            // Partner không có cột tên lĩnh vực denormalized → bắt buộc Include nav khi query.
            .ForMember(dest => dest.BusinessFieldName,
                opt => opt.MapFrom(src => src.BusinessField != null ? src.BusinessField.Name : null))
            // Company flat fields: prefer Company nav when included, fallback to legacy partner columns
            .ForMember(dest => dest.CompanyId, opt => opt.MapFrom(src => src.Company != null ? src.Company.Id : src.CompanyId))
            .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.Company != null ? src.Company.Name : src.CompanyName))
            .ForMember(dest => dest.CompanyTax, opt => opt.MapFrom(src => src.Company != null ? src.Company.TaxCode : src.CompanyTax))
            .ForMember(dest => dest.CompanyAddress, opt => opt.MapFrom(src => src.Company != null ? src.Company.Address : src.CompanyAddress))
            .ForMember(dest => dest.CompanyWebsite, opt => opt.MapFrom(src => src.Company != null ? src.Company.Website : src.CompanyWebsite))
            .ForMember(dest => dest.AccountReferrerCode,
                opt => opt.MapFrom(src => src.User != null ? src.User.AccountReferrerCode : null));
    }
}