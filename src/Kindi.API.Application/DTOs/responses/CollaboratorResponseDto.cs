using AutoMapper;
using Kindi.API.Application.Common.Helpers;
using Kindi.API.Application.Common.Mappings;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;

namespace Kindi.API.Application.DTOs.Responses;

public class CollaboratorResponseDto : IMapFrom<Collaborator>
{

    /// <summary>
    /// Chỉ có giá trị ở response của POST /Collaborators (đăng ký công khai):
    /// thông tin tài khoản vừa tạo/dùng lại để client hiển thị cho người đăng ký.
    /// </summary>
    public AccountCredentialsDto? Account { get; set; }
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Zalo { get; set; }
    public string? Email { get; set; }
    public string? Position { get; set; }
    public string? Skills { get; set; }
    public string? Interests { get; set; }
    public string? Goals { get; set; }
    public SalesChannel? SalesChannel { get; set; }
    public string? Experience { get; set; }
    public string? Address { get; set; }
    /// <summary>Id lĩnh vực kinh doanh (BusinessField).</summary>
    public Guid? BusinessFieldId { get; set; }
    /// <summary>Tên lĩnh vực kinh doanh.</summary>
    public string? BusinessFieldName { get; set; }
    public int Level { get; set; }
    public string? CollaboratorCode { get; set; }

    /// <summary>Mã chia sẻ riêng của CTV (dùng để gắn vào link chia sẻ).</summary>
    public string? ReferralCode { get; set; }

    /// <summary>Mã chia sẻ của người đã giới thiệu người tạo bản ghi (ghi nhận trên tài khoản).</summary>
    public string? AccountReferrerCode { get; set; }
    /// <summary>Tên CTV của <see cref="AccountReferrerCode"/>.</summary>
    public string? AccountReferrerName { get; set; }
    public CollaboratorStatus Status { get; set; }
    public bool IsApproved { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    // Company denormalized info (flat) returned for Get endpoints
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyTax { get; set; } = string.Empty;
    public string? CompanyAddress { get; set; }
    public string? CompanyWebsite { get; set; }
    public CompanyInfoDto? CompanyInfo { get; set; }

    public void Mapping(Profile profile)
        => profile.CreateMap<Collaborator, CollaboratorResponseDto>()
            // Thông tin cá nhân nằm ở bảng Users: lấy qua navigation User (null-safe cho query không Include).
            .ForMember(dest => dest.FullName,
                opt => opt.MapFrom(src => src.User != null ? src.User.FullName : string.Empty))
            .ForMember(dest => dest.Phone,
                opt => opt.MapFrom(src => src.User != null ? (src.User.Phone ?? string.Empty) : string.Empty))
            .ForMember(dest => dest.Zalo,
                opt => opt.MapFrom(src => src.User != null ? src.User.Zalo : null))
            // Email tạm hệ thống sinh ({sđt}@temp.com) coi như chưa có email → trả null.
            .ForMember(dest => dest.Email,
                opt => opt.MapFrom(src => src.User != null ? UserInfo.DisplayEmail(src.User.Email, src.User.Phone) : null))
            // Ưu tiên tên từ bảng BusinessFields (đổi tên vẫn đúng), fallback cột denormalized
            // để các bản ghi cũ / query không Include nav vẫn có dữ liệu.
            .ForMember(dest => dest.BusinessFieldName,
                opt => opt.MapFrom(src => src.BusinessField != null ? src.BusinessField.Name : src.BusinessFieldName))
            // Company flat fields: prefer joined Company nav when included, fallback to legacy columns
            .ForMember(dest => dest.CompanyId, opt => opt.MapFrom(src => src.Company != null ? src.Company.Id : src.CompanyId))
            .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.Company != null ? src.Company.Name : src.BusinessName))
            .ForMember(dest => dest.CompanyTax, opt => opt.MapFrom(src => src.Company != null ? src.Company.TaxCode : null))
            .ForMember(dest => dest.CompanyAddress, opt => opt.MapFrom(src => src.Company != null ? src.Company.Address : src.Address))
            .ForMember(dest => dest.CompanyWebsite, opt => opt.MapFrom(src => src.Company != null ? src.Company.Website : src.Website))
            .ForMember(dest => dest.CompanyInfo, opt => opt.MapFrom(src => src.Company != null ? new CompanyInfoDto
            {
                Id = src.Company.Id,
                CompanyName = src.Company.Name,
                CompanyTax = src.Company.TaxCode,
                CompanyAddress = src.Company.Address ?? src.Address,
                CompanyWebsite = src.Company.Website ?? src.Website,
                BusinessType = src.Company.BusinessType ?? null,
                CompanySize = src.Company.CompanySize ?? (src.BusinessSize.HasValue ? (CompanySize?)src.BusinessSize.Value : null),
                BusinessField = src.Company.BusinessField != null ? src.Company.BusinessField.Name : (src.BusinessField != null ? src.BusinessField.Name : src.BusinessFieldName)
            } : null))
            .ForMember(dest => dest.AccountReferrerCode,
                opt => opt.MapFrom(src => src.User != null ? src.User.AccountReferrerCode : null));
}