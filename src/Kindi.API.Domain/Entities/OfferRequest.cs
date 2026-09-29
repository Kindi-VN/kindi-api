using Kindi.API.Domain.Enums;

namespace Kindi.API.Domain.Entities;

public class OfferRequest : BaseEntity
{
    public string? OfferRequestCode { get; set; }
    public Guid UserId { get; set; }

    // Product information
    public string ProductName { get; set; } = string.Empty;
    public string? ProductLink { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal? ExpectedPrice { get; set; }
    public int Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;

    // User information
    public string? Note { get; set; }

    /// <summary>Mã CTV đã mang khách tới yêu cầu này (lấy từ link chia sẻ, ghi nhận 1 cấp).</summary>
    public string? ReferralCode { get; set; }

    // Status
    public OfferStatus Status { get; set; } = OfferStatus.Pending;
    public bool IsOfferSent { get; set; }

    public Guid? BusinessFieldId { get; set; }

    // Navigation
    public virtual User User { get; set; } = null!;
    public virtual BusinessField? BusinessField { get; set; } 
}