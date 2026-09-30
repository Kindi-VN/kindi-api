using Kindi.API.Domain.Enums;

namespace Kindi.API.Application.DTOs.requests;

public class GetPostsQuery
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public PostType? Type { get; set; }
    public PrivacyType? Privacy { get; set; }
    public string? Tag { get; set; }

    /// <summary>
    /// Chỉ lấy bài viết của chính người gọi (khu vực thành viên) — mọi trạng thái duyệt.
    /// </summary>
    public bool MineOnly { get; set; }

    /// <summary>
    /// Lọc theo trạng thái duyệt: true = đã duyệt, false = chờ duyệt.
    /// </summary>
    public bool? IsApproved { get; set; }
}