// PartnerRegisterRequest.cs
using Kindi.API.Application.Common.Mappings;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using AutoMapper;

namespace Kindi.API.Application.DTOs.Requests;

public class PartnerRegisterRequest : IMapFrom<Partner>
{
    // Step 1: Personal Info (+ mã giới thiệu)
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string? RecordReferrerCode { get; set; }

    // Step 2: Business Info (lĩnh vực hoạt động quản lý tập trung qua BusinessField)
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyAddress { get; set; } = string.Empty;
    public string? CompanyTax { get; set; } = string.Empty;
    public string? CompanyWebsite { get; set; }
    public Guid? BusinessFieldId { get; set; }
    public CompanySize CompanySize { get; set; }

    // Step 2: Sản phẩm & dịch vụ cung cấp (tối giản: name + description)
    public List<ProductDto> Products { get; set; } = new();

    // Step 3: Confirmation
    public bool AgreeTerms { get; set; }

    public void Mapping(Profile profile)
    {
        // Map PartnerRegisterRequest -> Partner
        profile.CreateMap<PartnerRegisterRequest, Partner>()
            .ForMember(dest => dest.PartnerCode,
                opt => opt.MapFrom(src => $"KINDI-{Guid.NewGuid():N}".Substring(0, 8).ToUpper()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => PartnerStatus.Pending))
            // Không thu thập BusinessType ở form mới — giữ enum hợp lệ để tương thích hiển thị cũ
            .ForMember(dest => dest.BusinessType, opt => opt.MapFrom(src => BusinessType.Other))
            .ForMember(dest => dest.Products, opt => opt.Ignore()) // Xử lý riêng
            .ForMember(dest => dest.Commission, opt => opt.Ignore()); // Xử lý riêng

        // Map ProductDto -> PartnerProduct
        profile.CreateMap<ProductDto, PartnerProduct>();
    }
}