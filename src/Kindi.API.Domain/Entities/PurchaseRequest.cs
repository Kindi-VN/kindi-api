using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;

namespace Kindi.API.Domain.Entities;

public class PurchaseRequest : BaseEntity
{
    public string? PurchaseRequestCode { get; set; }
    public Guid UserId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductCategory { get; set; }
    public int Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal? ExpectedPrice { get; set; }
    public string? Note { get; set; }

    /// <summary>Mã CTV đã mang khách tới yêu cầu này (lấy từ link chia sẻ, ghi nhận 1 cấp).</summary>
    public string? ReferralCode { get; set; }

    public PurchaseRequestStatus Status { get; set; } = PurchaseRequestStatus.Pending;
    public string? AdminNote { get; set; }
    public Guid? AssignedTo { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? Source { get; set; }

    public Guid? BusinessFieldId { get; set; }

    // Navigation
    public virtual User User { get; set; } = null!;
    public virtual BusinessField? BusinessField { get; set; }
}