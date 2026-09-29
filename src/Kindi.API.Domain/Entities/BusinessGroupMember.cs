using Kindi.API.Domain.Enums;

namespace Kindi.API.Domain.Entities;

/// <summary>
/// Một thành viên của nhóm theo lĩnh vực. Thành viên phải có tài khoản:
/// khách chưa có tài khoản thì hệ thống tạo từ thông tin liên hệ (như luồng mua chung).
/// </summary>
public class BusinessGroupMember : BaseEntity
{
	/// <summary>Mã thành viên nhóm hiển thị cho người dùng.</summary>
	public string? BusinessGroupMemberCode { get; set; }

	public Guid BusinessGroupId { get; set; }
	public Guid UserId { get; set; }

	public string? Note { get; set; }

	/// <summary>Mã CTV của link mà người này dùng để xin vào nhóm (ghi nhận 1 cấp).</summary>
	public string? ReferralCode { get; set; }

	public GroupMemberRole Role { get; set; } = GroupMemberRole.Member;
	public GroupMemberStatus Status { get; set; } = GroupMemberStatus.Pending;

	/// <summary>Tài khoản được tạo tự động từ thông tin liên hệ của khách.</summary>
	public bool IsGuestAccount { get; set; }

	public DateTime? JoinedAt { get; set; }
	public DateTime? ApprovedAt { get; set; }
	public Guid? ApprovedByUserId { get; set; }
	public string? RejectionReason { get; set; }

	// Navigation
	public virtual BusinessGroup BusinessGroup { get; set; } = null!;
	public virtual User User { get; set; } = null!;
}
