using AutoMapper;
using Kindi.API.Application.Common.Mappings;
using Kindi.API.Domain.Entities;
using System.Text.Json.Serialization;

namespace Kindi.API.Application.DTOs.requests;

public class CreateOfferRequestDto : IMapFrom<OfferRequest>
{
    // Product information
    public string ProductName { get; set; } = string.Empty;
    public string? ProductLink { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal? ExpectedPrice { get; set; }
    public int Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;

    // User information
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Zalo { get; set; }
    public string? Email { get; set; }
    public string? Note { get; set; }

    /// <summary>Mã CTV của link chia sẻ khách dùng để tạo yêu cầu (không bắt buộc).</summary>
    public string? RecordReferrerCode { get; set; }

    /// <summary>
    /// Nhịp chuyển tiếp: tên cũ "referralCode" của <see cref="RecordReferrerCode"/>.
    /// Chỉ để đọc dữ liệu client cũ; điền vào trường chính khi trường chính còn trống,
    /// còn trường chính đã có giá trị thì bỏ qua tên cũ (tên chính luôn được ưu tiên).
    /// </summary>
    [JsonPropertyName("referralCode")]
    public string? LegacyReferralCode
    {
        set
        {
            if (string.IsNullOrWhiteSpace(RecordReferrerCode))
                RecordReferrerCode = value;
        }
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CreateOfferRequestDto, OfferRequest>();
    }
}