namespace Kindi.API.Application.DTOs.requests;

/// <summary>
/// Admin đổi trạng thái yêu cầu mua chung (duyệt / đóng / hủy).
/// </summary>
public class UpdateGroupBuyingStatusDto
{
    /// <summary>Giá trị của <see cref="Kindi.API.Domain.Enums.GroupBuyingStatus"/> (1=Pending, 2=Active, 3=Completed, 4=Cancelled).</summary>
    public int Status { get; set; }

    /// <summary>Lý do (bắt buộc khi hủy, tùy chọn khi hoàn thành).</summary>
    public string? Reason { get; set; }
}
