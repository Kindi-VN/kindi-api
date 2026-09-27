using Kindi.API.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kindi.API.Domain.Entities;

[Table("Companies")]
public class Company : BaseEntity
{
    /// <summary>Mã công ty hiển thị cho người dùng.</summary>
    public string? CompanyCode { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? TaxCode { get; set; }
    public string? Address { get; set; }
    public string? Website { get; set; }
    public BusinessType? BusinessType { get; set; }
    public CompanySize? CompanySize { get; set; }

    public Guid? BusinessFieldId { get; set; }

    // Navigation
    public virtual BusinessField? BusinessField { get; set; }
}
