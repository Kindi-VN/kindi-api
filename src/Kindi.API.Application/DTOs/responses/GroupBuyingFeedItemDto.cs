using Kindi.API.Domain.Enums;

namespace Kindi.API.Application.DTOs.responses;

/// <summary>
/// Item hiển thị trên tab "Mua chung" (trang social) — chỉ thông tin cơ bản + tiến độ số người.
/// </summary>
public class GroupBuyingFeedItemDto
{
    public Guid Id { get; set; }
    public string? GroupBuyingRequestCode { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductLink { get; set; }
    public decimal? TargetPrice { get; set; }
    public int TargetPeopleCount { get; set; }
    public int CurrentPeopleCount { get; set; }
    /// <summary>Số người còn thiếu để đạt mục tiêu (không âm).</summary>
    public int NeededPeopleCount { get; set; }
    public GroupBuyingStatus Status { get; set; }
    public string? Note { get; set; }
    public string? BusinessFieldName { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsMine { get; set; }
    public bool IsJoinedByMe { get; set; }
    public bool CanJoin { get; set; }
    /// <summary>Tên những người đã tham gia (đã rút gọn) để hiển thị nhanh.</summary>
    public List<string> Participants { get; set; } = new();
}
