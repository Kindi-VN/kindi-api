using Kindi.API.Domain.Enums;

namespace Kindi.API.Application.DTOs.requests;

/// <summary>
/// Bài đăng trong nhóm: thành viên thảo luận hoặc gửi yêu cầu kín cho admin;
/// admin gửi offer / yêu cầu mua chung / yêu cầu tìm nhà cung cấp / thông báo.
/// </summary>
public class CreateBusinessGroupPostDto
{
    public string? Title { get; set; }
    public string Content { get; set; } = string.Empty;

    public GroupPostType Type { get; set; } = GroupPostType.Discussion;

    /// <summary>Id bản ghi được chia sẻ vào nhóm (offer / yêu cầu mua chung / yêu cầu tìm cung cấp).</summary>
    public Guid? RefId { get; set; }
    public string? RefCode { get; set; }

    /// <summary>Yêu cầu kín: chỉ admin đọc được.</summary>
    public bool IsPrivateToAdmin { get; set; }

    /// <summary>Bài chuyển tiếp kèm link chia sẻ để mời thành viên tham gia bản ghi gốc.</summary>
    public bool WithShareLink { get; set; }
}
