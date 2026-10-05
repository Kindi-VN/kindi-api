using Kindi.API.Domain.Enums;

namespace Kindi.API.Domain.Entities;

public class Partner : BaseEntity
{
    public Guid UserId { get; set; }
    public string PartnerCode { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyTax { get; set; } = string.Empty;
    public string CompanyAddress { get; set; } = string.Empty;
    public BusinessType BusinessType { get; set; }
    public CompanySize CompanySize { get; set; }
    public string? CompanyWebsite { get; set; }
    // Link to shared company table (new centralized company info)
    public Guid? CompanyId { get; set; }
    public virtual Company? Company { get; set; }
    public string? Note { get; set; }
    public PartnerStatus Status { get; set; } = PartnerStatus.Pending;
    public DateTime? ApprovedAt { get; set; }

    public Guid? BusinessFieldId { get; set; }

    // Navigation
    public virtual User User { get; set; } = null!;
    public virtual ICollection<PartnerProduct> Products { get; set; } = new List<PartnerProduct>();
    public virtual PartnerCommission? Commission { get; set; }
    public virtual BusinessField? BusinessField { get; set; } 
}