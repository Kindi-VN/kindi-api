using Kindi.API.Domain.Enums;

namespace Kindi.API.Application.DTOs.requests;

public class GetPostsQuery
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public PostType? Type { get; set; }
    public PrivacyType? Privacy { get; set; }
    public string? Tag { get; set; }

    /// <summary>Từ khoá tìm theo nội dung bài viết, họ tên tác giả hoặc mã tài khoản tác giả.</summary>
    public string? Search { get; set; }

    /// <summary>
    /// Chỉ tìm theo đúng một trường (không OR lan sang cột khác); bỏ trống = tìm cả nội dung lẫn tác giả.
    /// Giá trị hợp lệ: content, authorFullName, authorUserCode.
    /// </summary>
    public SocialPostSearchField? SearchField { get; set; }

    /// <summary>
    /// Chỉ lấy bài viết của chính người gọi (khu vực thành viên) — mọi trạng thái duyệt.
    /// </summary>
    public bool MineOnly { get; set; }

    /// <summary>
    /// Lọc theo trạng thái duyệt: true = đã duyệt, false = chờ duyệt.
    /// </summary>
    public bool? IsApproved { get; set; }
}