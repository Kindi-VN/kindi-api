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

    /// <summary>true = chỉ lấy các nhóm do chính người dùng hiện tại mở (kể cả đang chờ duyệt).</summary>
    public bool MineOnly { get; set; }
}
