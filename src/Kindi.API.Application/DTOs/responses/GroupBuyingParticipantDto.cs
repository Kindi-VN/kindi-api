// src/Kindi.API.Application/DTOs/responses/GroupBuyingParticipantDto.cs
using Kindi.API.Domain.Enums;

namespace Kindi.API.Application.DTOs.responses;

/// <summary>
/// Một người tham gia nhóm mua chung. Thông tin liên hệ đã được che (mask) trừ admin
/// và chính người đang xem.
/// </summary>
public class GroupBuyingParticipantDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? UserCode { get; set; }
    public string? CollaboratorCode { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Zalo { get; set; }
    public string? Email { get; set; }
    public string? Note { get; set; }
    public bool IsCreator { get; set; }
    public bool IsGuestAccount { get; set; }
    public GroupBuyingParticipantStatus Status { get; set; }
    /// <summary>true nếu bản ghi này là của người đang gọi API.</summary>
    public bool IsMe { get; set; }
    public DateTime CreatedAt { get; set; }
}
