using Kindi.API.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kindi.API.Domain.Entities;

[Table("Collaborators")]
public class Collaborator : BaseEntity
{
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
    public bool AgreeTerms { get; set; }
    public string? BusinessFieldName { get; set; }
    public Guid? BusinessFieldId { get; set; }
    // Thông tin doanh nghiệp (lấy từ form đăng ký CTV)
    public string? BusinessName { get; set; }
    public int? BusinessSize { get; set; }
    public string? Website { get; set; }
    // Link to shared company table
    public Guid? CompanyId { get; set; }
    public virtual Company? Company { get; set; }
    // Hỗ trợ đa cấp
    public Guid? ParentCollaboratorId { get; set; }
    public int Level { get; set; } = 1;
    public string? CollaboratorCode { get; set; }

    /// <summary>Mã CTV đã giới thiệu CTV này (lấy từ link chia sẻ, ghi nhận 1 cấp).</summary>
    public string? ReferralCode { get; set; }

    public bool IsApproved { get; set; } = false;
    private CollaboratorStatus _status = CollaboratorStatus.Pending;
    public CollaboratorStatus Status
    {
        get => _status;
        set
        {
            _status = value;
            IsApproved = value == CollaboratorStatus.Approved;
        }
    }

    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }

    // Navigation
    public virtual User User { get; set; } = null!;
    public virtual Collaborator? ParentCollaborator { get; set; }
    public virtual ICollection<Collaborator> Children { get; set; } = new List<Collaborator>();
    public virtual ICollection<Partner> Partners { get; set; } = new List<Partner>();
    public virtual BusinessField? BusinessField { get; set; } 
}