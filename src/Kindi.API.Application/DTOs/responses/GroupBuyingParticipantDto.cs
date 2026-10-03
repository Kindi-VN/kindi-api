using Kindi.API.Domain.Enums;

namespace Kindi.API.Application.DTOs.responses;

/// <summary>
/// Một người tham gia nhóm mua chung. Thông tin liên hệ đã được che (mask) trừ admin
/// và chính người đang xem.
/// </summary>
public class GroupBuyingParticipantDto
{
    public Guid Id { get; set; }
    /// <summary>Mã người tham gia mua chung hiển thị cho người dùng.</summary>
    public string? GroupBuyingParticipantCode { get; set; }
    public Guid UserId { get; set; }
    public string? UserCode { get; set; }
    public string? CollaboratorCode { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Zalo { get; set; }
    public string? Email { get; set; }
    public string? Note { get; set; }
    /// <summary>Mã CTV đã mang người này vào nhóm (lấy từ link chia sẻ).</summary>
    public string? ReferralCode { get; set; }
    /// <summary>Tên CTV của <see cref="ReferralCode"/>.</summary>
    public string? ReferralName { get; set; }

    /// <summary>Mã chia sẻ của người đã giới thiệu người tạo bản ghi (ghi nhận trên tài khoản).</summary>
    public string? ReferredByCode { get; set; }
    /// <summary>Tên CTV của <see cref="ReferredByCode"/>.</summary>
    public string? ReferredByName { get; set; }
    public bool IsCreator { get; set; }
    public bool IsGuestAccount { get; set; }
    public GroupBuyingParticipantStatus Status { get; set; }
    /// <summary>true nếu bản ghi này là của người đang gọi API.</summary>
    public bool IsMe { get; set; }
    public DateTime CreatedAt { get; set; }
}
