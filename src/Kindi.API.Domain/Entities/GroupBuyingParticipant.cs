using Kindi.API.Domain.Enums;

namespace Kindi.API.Domain.Entities;

/// <summary>
/// Một người đăng ký tham gia yêu cầu mua chung.
/// Người mở yêu cầu cũng có 1 bản ghi với <see cref="IsCreator"/> = true (chiếm slot đầu tiên của nhóm).
/// </summary>
public class GroupBuyingParticipant : BaseEntity
{
	/// <summary>Mã người tham gia mua chung hiển thị cho người dùng.</summary>
	public string? GroupBuyingParticipantCode { get; set; }

	public Guid GroupBuyingRequestId { get; set; }
	public Guid UserId { get; set; }
	public string FullName { get; set; } = string.Empty;
	public string Phone { get; set; } = string.Empty;
	public string? Zalo { get; set; }
	public string? Email { get; set; }
	public string? Note { get; set; }

	/// <summary>Mã CTV của link mà người này dùng để tham gia (ghi nhận 1 cấp).</summary>
	public string? ReferralCode { get; set; }

	/// <summary>Người mở yêu cầu mua chung — admin không thể xóa khỏi nhóm.</summary>
	public bool IsCreator { get; set; }

	/// <summary>Bản ghi sinh ra từ khách chưa có tài khoản (hệ thống tự tạo tài khoản từ thông tin liên hệ).</summary>
	public bool IsGuestAccount { get; set; }

	public GroupBuyingParticipantStatus Status { get; set; } = GroupBuyingParticipantStatus.Joined;

	// Navigation
	public virtual GroupBuyingRequest GroupBuyingRequest { get; set; } = null!;
	public virtual User User { get; set; } = null!;
}
