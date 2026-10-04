using Kindi.API.Domain.Enums;

namespace Kindi.API.Domain.Entities;

public class GroupBuyingRequest : BaseEntity
{
    public string? GroupBuyingRequestCode { get; set; }
    public Guid UserId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductLink { get; set; }
    public int TargetPeopleCount { get; set; }
    public int CurrentPeopleCount { get; set; }
    public decimal? TargetPrice { get; set; }
    public string? Note { get; set; }

    /// <summary>Mã CTV đã mang khách tới yêu cầu này (lấy từ link chia sẻ, ghi nhận 1 cấp).</summary>
    public string? RecordReferrerCode { get; set; }

    public GroupBuyingStatus Status { get; set; } = GroupBuyingStatus.Pending;

    public Guid? BusinessFieldId { get; set; }

    // ===== Duyệt / đóng nhóm (admin) =====
    /// <summary>Thời điểm admin duyệt nhóm (Pending → Active).</summary>
    public DateTime? ApprovedAt { get; set; }
    /// <summary>User id của admin duyệt nhóm (lưu vết, không ràng buộc FK).</summary>
    public Guid? ApprovedByUserId { get; set; }
    /// <summary>Lý do đóng/hủy nhóm (admin nhập khi chuyển sang Completed/Cancelled).</summary>
    public string? ClosedReason { get; set; }

    // Navigation
    public virtual User User { get; set; } = null!;
    public virtual BusinessField? BusinessField { get; set; }
    public virtual ICollection<GroupBuyingParticipant> Participants { get; set; } = new List<GroupBuyingParticipant>();
}
