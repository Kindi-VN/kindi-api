using Kindi.API.Domain.Enums;

namespace Kindi.API.Domain.Entities;

/// <summary>
/// Bài đăng trong nhóm: thành viên thảo luận như 1 bài post, admin gửi offer /
/// yêu cầu mua chung / yêu cầu tìm nhà cung cấp; thành viên có thể gửi yêu cầu kín cho admin.
/// </summary>
public class BusinessGroupPost : BaseEntity
{
	public string? BusinessGroupPostCode { get; set; }
	public Guid BusinessGroupId { get; set; }
	public Guid AuthorId { get; set; }

	public string? Title { get; set; }
	public string Content { get; set; } = string.Empty;

	public GroupPostType Type { get; set; } = GroupPostType.Discussion;

	/// <summary>Id bản ghi được chia sẻ vào nhóm (offer / yêu cầu mua chung / yêu cầu tìm cung cấp).</summary>
	public Guid? RefId { get; set; }
	/// <summary>Mã hiển thị của bản ghi được chia sẻ (ví dụ GBR-XXXXXX, OFR-XXXXXX).</summary>
	public string? RefCode { get; set; }

	/// <summary>Mã CTV của người chuyển tiếp bản ghi vào nhóm (ghi nhận 1 cấp).</summary>
	public string? RecordReferrerCode { get; set; }

	/// <summary>Bài chuyển tiếp kèm link chia sẻ để mời thành viên tham gia bản ghi gốc.</summary>
	public bool WithShareLink { get; set; }

	/// <summary>true = yêu cầu kín: chỉ admin đọc được (thành viên gửi riêng cho admin).</summary>
	public bool IsPrivateToAdmin { get; set; }

	public bool IsPinned { get; set; }
	public bool IsHidden { get; set; }

	public int CommentsCount { get; set; }

	// Navigation
	public virtual BusinessGroup BusinessGroup { get; set; } = null!;
	public virtual User Author { get; set; } = null!;
	public virtual ICollection<BusinessGroupComment> Comments { get; set; } = new List<BusinessGroupComment>();
}
