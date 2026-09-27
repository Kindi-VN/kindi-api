namespace Kindi.API.Domain.Entities;

/// <summary>Bình luận trong bài của nhóm.</summary>
public class BusinessGroupComment : BaseEntity
{
	/// <summary>Mã bình luận hiển thị cho người dùng.</summary>
	public string? BusinessGroupCommentCode { get; set; }

	public Guid BusinessGroupPostId { get; set; }
	public Guid UserId { get; set; }
	public string Content { get; set; } = string.Empty;
	public Guid? ParentCommentId { get; set; }
	public bool IsHidden { get; set; }

	// Navigation
	public virtual BusinessGroupPost Post { get; set; } = null!;
	public virtual User User { get; set; } = null!;
	public virtual BusinessGroupComment? ParentComment { get; set; }
	public virtual ICollection<BusinessGroupComment> Replies { get; set; } = new List<BusinessGroupComment>();
}
