using AutoMapper;
using Kindi.API.Application.Common.Mappings;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;

namespace Kindi.API.Application.DTOs.Requests;

public class CreateCollaboratorDto : IMapFrom<Collaborator>
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Zalo { get; set; }
    public string? Email { get; set; }
    public string? Username { get; set; }          // Thêm
    public string? Password { get; set; }          // Thêm
    public string? ConfirmPassword { get; set; }   // Thêm
    public string? Role { get; set; }              // Thêm
    public string? Position { get; set; }
    public string? Skills { get; set; }
    public string? Interests { get; set; }
    public string? Goals { get; set; }
    public SalesChannel? SalesChannel { get; set; }
    public string? Experience { get; set; }
    public bool AgreeTerms { get; set; }

    // Thông tin doanh nghiệp (thêm)
    public string? BusinessName { get; set; }
    public string? CompanyTax { get; set; } = string.Empty;
    /// <summary>Id lĩnh vực hoạt động (BusinessField). Được ưu tiên hơn BusinessFieldName.</summary>
    public Guid? BusinessFieldId { get; set; }
    /// <summary>Tên lĩnh vực — fallback find-or-create cho client chưa gửi BusinessFieldId.</summary>
    public string? BusinessFieldName { get; set; }
    public int? BusinessSize { get; set; }
    public string? Address { get; set; }
    public string? Website { get; set; }

    public Guid? ParentCollaboratorId { get; set; }

    /// <summary>
    /// Mã chia sẻ trên link (?ref=) — mã của người giới thiệu tài khoản đăng ký.
    /// Không phải mã chia sẻ của hồ sơ CTV (mã đó lấy theo CollaboratorCode).
    /// </summary>
    public string? AccountReferrerCode { get; set; }

    public void Mapping(Profile profile)
      => profile.CreateMap<CreateCollaboratorDto, Collaborator>()
          .ForMember(dest => dest.IsApproved, opt => opt.MapFrom(src => false))
          .ForMember(dest => dest.Status, opt => opt.MapFrom(src => CollaboratorStatus.Pending))
          .ForMember(dest => dest.Level, opt => opt.MapFrom(src => 1));
}