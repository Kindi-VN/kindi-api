using AutoMapper;
using Kindi.API.Application.Common.Mappings;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;

namespace Kindi.API.Application.DTOs.responses;

public class BusinessGroupMemberResponseDto : IMapFrom<BusinessGroupMember>
{
    public Guid Id { get; set; }
    /// <summary>Mã thành viên nhóm hiển thị cho người dùng.</summary>
    public string? BusinessGroupMemberCode { get; set; }
    public Guid BusinessGroupId { get; set; }
    public Guid UserId { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Zalo { get; set; }
    public string? Email { get; set; }
    public string? Note { get; set; }

    /// <summary>Mã CTV đã mang người này vào nhóm (lấy từ link chia sẻ).</summary>
    public string? RecordReferrerCode { get; set; }
    /// <summary>Tên CTV của <see cref="RecordReferrerCode"/>.</summary>
    public string? RecordReferrerName { get; set; }

    /// <summary>Mã chia sẻ của người đã giới thiệu người tạo bản ghi (ghi nhận trên tài khoản).</summary>
    public string? AccountReferrerCode { get; set; }
    /// <summary>Tên CTV của <see cref="AccountReferrerCode"/>.</summary>
    public string? AccountReferrerName { get; set; }

    public GroupMemberRole Role { get; set; }
    public GroupMemberStatus Status { get; set; }
    public bool IsGuestAccount { get; set; }

    public DateTime? JoinedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
}
