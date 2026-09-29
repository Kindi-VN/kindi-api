using AutoMapper;
using Kindi.API.Application.Common.Mappings;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;

namespace Kindi.API.Application.DTOs.Responses;

/// <summary>
/// Đối tác doanh nghiệp đã đăng ký với Kindi và đã được duyệt — dùng cho trang Nguồn cung.
/// Chỉ trả thông tin cấp doanh nghiệp + người liên hệ; KHÔNG trả SĐT/email (liên hệ qua form của app).
/// </summary>
public class PublicPartnerResponseDto : IMapFrom<Partner>
{
    public Guid Id { get; set; }
    public string PartnerCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? BusinessFieldName { get; set; }
    public BusinessType BusinessType { get; set; }
    public CompanySize CompanySize { get; set; }
    public string? CompanyWebsite { get; set; }
    public string? CompanyAddress { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string? Position { get; set; }
    public List<string> Products { get; set; } = new();
    public DateTime CreatedAt { get; set; }

    public void Mapping(Profile profile)
        => profile.CreateMap<Partner, PublicPartnerResponseDto>()
            .ForMember(dest => dest.ContactName,
                opt => opt.MapFrom(src => src.User != null ? src.User.FullName : string.Empty))
            .ForMember(dest => dest.BusinessFieldName,
                opt => opt.MapFrom(src => src.BusinessField != null ? src.BusinessField.Name : null))
            .ForMember(dest => dest.Products,
                opt => opt.MapFrom(src => src.Products
                    .Where(p => !p.IsDeleted)
                    .Select(p => p.Name)
                    .ToList()));
}
