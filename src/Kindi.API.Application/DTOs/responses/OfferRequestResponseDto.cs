using AutoMapper;
using Kindi.API.Application.Common.Helpers;
using Kindi.API.Application.Common.Mappings;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;

namespace Kindi.API.Application.DTOs.responses;

public class OfferRequestResponseDto : IMapFrom<OfferRequest>
{
	public Guid Id { get; set; }
	public string? OfferRequestCode { get; set; }
	public Guid UserId { get; set; }

    /// <summary>Mã tài khoản (USR-…) của người gửi/lập yêu cầu.</summary>
    public string? UserCode { get; set; }
	public string ProductName { get; set; } = string.Empty;
	public string? ProductLink { get; set; }
	public decimal CurrentPrice { get; set; }
	public decimal? ExpectedPrice { get; set; }
	public int Quantity { get; set; }
	public string Unit { get; set; } = string.Empty;
	public string FullName { get; set; } = string.Empty;
	public string Phone { get; set; } = string.Empty;
	public string Zalo { get; set; } = string.Empty;
	public string? Email { get; set; }
	public string? Note { get; set; }

    /// <summary>Mã CTV đã mang khách tới yêu cầu này (lấy từ link chia sẻ).</summary>
    public string? RecordReferrerCode { get; set; }

    /// <summary>Tên chủ thể của mã chia sẻ (CTV hoặc tài khoản) — hiển thị ở màn quản trị.</summary>
    public string? RecordReferrerName { get; set; }

    /// <summary>Mã chia sẻ của người đã giới thiệu người tạo bản ghi (ghi nhận trên tài khoản).</summary>
    public string? AccountReferrerCode { get; set; }
    /// <summary>Tên CTV của <see cref="AccountReferrerCode"/>.</summary>
    public string? AccountReferrerName { get; set; }
	public OfferStatus Status { get; set; }
	public bool IsOfferSent { get; set; }
	public Guid? BusinessFieldId { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime? UpdatedAt { get; set; }

	public void Mapping(Profile profile)
	{
		// Thông tin cá nhân nằm ở bảng Users — lấy qua navigation User khi map DTO.
		profile.CreateMap<OfferRequest, OfferRequestResponseDto>()
			.ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : string.Empty))
			.ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.User != null ? src.User.Phone : string.Empty))
			.ForMember(dest => dest.Zalo, opt => opt.MapFrom(src => src.User != null ? (src.User.Zalo ?? string.Empty) : string.Empty))
			.ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User != null ? UserInfo.DisplayEmail(src.User.Email, src.User.Phone) : null))
			.ForMember(dest => dest.AccountReferrerCode,
			    opt => opt.MapFrom(src => src.User != null ? src.User.AccountReferrerCode : null))
			.ForMember(dest => dest.UserCode,
			    opt => opt.MapFrom(src => src.User != null ? src.User.UserCode : null));
	}
}