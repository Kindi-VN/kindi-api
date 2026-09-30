using Kindi.API.Domain.Enums;

namespace Kindi.API.Application.DTOs.requests;

/// <summary>
/// Query cho danh sách mua chung công khai (tab Mua chung trên trang social).
/// </summary>
public class GetPublicGroupBuyingRequestsQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public string? Search { get; set; }
    public string? SortBy { get; set; }    // VD: "CreatedAt"
    public string? SortOrder { get; set; } // "asc" | "desc"

    /// <summary>true = chỉ lấy các nhóm do chính người dùng hiện tại mở (mọi trạng thái, kể cả đã hoàn thành/đã hủy).</summary>
    public bool MineOnly { get; set; }

    /// <summary>Lọc theo trạng thái (dùng cho tab trong khu vực thành viên); bỏ trống = tất cả trạng thái được phép xem.</summary>
    public GroupBuyingStatus? Status { get; set; }
}
