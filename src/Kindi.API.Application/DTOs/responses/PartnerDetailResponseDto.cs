using Kindi.API.Application.Common.Helpers;
using Kindi.API.Application.Common.Mappings;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using AutoMapper;

namespace Kindi.API.Application.DTOs.Responses;

public class PartnerDetailResponseDto : IMapFrom<Partner>
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
    public BusinessType BusinessType { get; set; }
    public CompanySize CompanySize { get; set; }
    public string? CompanyWebsite { get; set; }
    public string? ReferralCode { get; set; }

    /// <summary>Mã chia sẻ của người đã giới thiệu người tạo bản ghi (ghi nhận trên tài khoản).</summary>
    public string? ReferredByCode { get; set; }
    /// <summary>Tên CTV của <see cref="ReferredByCode"/>.</summary>
    public string? ReferredByName { get; set; }
    public string? Note { get; set; }
    public PartnerStatus Status { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public UserBriefDto? User { get; set; }
    public PartnerCommissionDto? Commission { get; set; }
    public List<PartnerProductDto> Products { get; set; } = new();
    public BusinessInfoDto? BusinessInfo { get; set; }
    public CompanyInfoDto? CompanyInfo { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Partner, PartnerDetailResponseDto>()
            // Thông tin cá nhân chỉ nằm ở bảng Users → lấy qua nav User (bắt buộc Include khi query).
            .ForMember(dest => dest.FullName,
                opt => opt.MapFrom(src => src.User != null ? src.User.FullName : string.Empty))
            .ForMember(dest => dest.Phone,
                opt => opt.MapFrom(src => src.User != null ? (src.User.Phone ?? string.Empty) : string.Empty))
            .ForMember(dest => dest.Email,
                opt => opt.MapFrom(src => (src.User != null ? UserInfo.DisplayEmail(src.User.Email, src.User.Phone) : null) ?? string.Empty))
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User))
            .ForMember(dest => dest.Commission, opt => opt.MapFrom(src => src.Commission))
            .ForMember(dest => dest.Products, opt => opt.MapFrom(src => src.Products))
            .ForMember(dest => dest.BusinessInfo, opt => opt.MapFrom(src => new BusinessInfoDto
            {
                CompanyName = src.CompanyName,
                CompanyTax = src.CompanyTax,
                CompanyAddress = src.CompanyAddress,
                CompanyWebsite = src.CompanyWebsite,
                BusinessType = src.BusinessType,
                CompanySize = src.CompanySize,
                BusinessField = src.BusinessField != null ? src.BusinessField.Name : null
            }))
            .ForMember(dest => dest.CompanyInfo, opt => opt.MapFrom(src => src.Company != null ? new CompanyInfoDto
            {
                Id = src.Company.Id,
                CompanyName = src.Company.Name,
                CompanyTax = src.Company.TaxCode,
                CompanyAddress = src.Company.Address,
                CompanyWebsite = src.Company.Website,
                BusinessType = src.Company.BusinessType ?? src.BusinessType,
                CompanySize = src.Company.CompanySize ?? src.CompanySize,
                BusinessField = src.Company.BusinessField != null ? src.Company.BusinessField.Name : (src.BusinessField != null ? src.BusinessField.Name : null)
            } : null))
            .ForMember(dest => dest.ReferredByCode,
                opt => opt.MapFrom(src => src.User != null ? src.User.ReferredByCode : null));

        // ✅ Mapping cho các DTO con
        profile.CreateMap<User, UserBriefDto>();
        profile.CreateMap<PartnerCommission, PartnerCommissionDto>();
        profile.CreateMap<PartnerProduct, PartnerProductDto>()
            .ForMember(dest => dest.BusinessFieldName,
                opt => opt.MapFrom(src => src.BusinessField != null ? src.BusinessField.Name : null));
    }

}





public class UserBriefDto
{
    public Guid Id { get; set; }
    public string? UserCode { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public class PartnerCommissionDto
{
    public Guid Id { get; set; }
    public string? PartnerCommissionCode { get; set; }
    public CommissionType Type { get; set; }
    public decimal Rate { get; set; }
    public decimal? MinOrderValue { get; set; }
    public decimal? MaxCommission { get; set; }
    public string? SpecialConditions { get; set; }
}

public class PartnerProductDto
{
    public Guid Id { get; set; }
    public string? PartnerProductCode { get; set; }
    public Guid PartnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProductCategory Category { get; set; }
    public decimal RetailPrice { get; set; }
    public decimal WholesalePrice { get; set; }
    public int MinOrderQuantity { get; set; }

    /// <summary>Lĩnh vực kinh doanh của sản phẩm.</summary>
    public Guid? BusinessFieldId { get; set; }
    /// <summary>Tên lĩnh vực — lấy từ nav BusinessField (cần Include khi query).</summary>
    public string? BusinessFieldName { get; set; }
}