// src/Kindi.API.Application/DTOs/responses/GroupBuyingDetailDto.cs
using Kindi.API.Domain.Enums;

namespace Kindi.API.Application.DTOs.responses;

/// <summary>
/// Chi tiết yêu cầu mua chung. Thông tin liên hệ chỉ hiển thị đầy đủ với admin;
/// người dùng khác chỉ thấy liên hệ của chính mình.
/// </summary>
public class GroupBuyingDetailDto
{
    public Guid Id { get; set; }
    public string? GroupBuyingRequestCode { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductLink { get; set; }
    public decimal? TargetPrice { get; set; }
    public int TargetPeopleCount { get; set; }
    public int CurrentPeopleCount { get; set; }
    public int NeededPeopleCount { get; set; }
    public GroupBuyingStatus Status { get; set; }
    public string? Note { get; set; }
    public Guid? BusinessFieldId { get; set; }
    public string? BusinessFieldName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ClosedReason { get; set; }

    /// <summary>Mã CTV đã mang khách tới yêu cầu này (lấy từ link chia sẻ).</summary>
    public string? ReferralCode { get; set; }
    /// <summary>Tên CTV của <see cref="ReferralCode"/>.</summary>
    public string? ReferralName { get; set; }

    // Người mở nhóm (đầu mối liên hệ)
    public string CreatorName { get; set; } = string.Empty;
    public string CreatorPhone { get; set; } = string.Empty;
    public string? CreatorZalo { get; set; }
    public string? CreatorEmail { get; set; }

    public bool IsMine { get; set; }
    public bool IsJoinedByMe { get; set; }
    public bool CanJoin { get; set; }
    /// <summary>Lý do không thể tham gia (đã full, chưa duyệt, đã đóng, là người mở nhóm...).</summary>
    public string? JoinBlockedReason { get; set; }

    public List<GroupBuyingParticipantDto> Participants { get; set; } = new();
}
