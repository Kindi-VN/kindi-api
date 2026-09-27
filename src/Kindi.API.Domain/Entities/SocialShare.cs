using System;

namespace Kindi.API.Domain.Entities;

public class SocialShare : BaseEntity
{
    /// <summary>Mã lượt chia sẻ bài viết hiển thị cho người dùng.</summary>
    public string? SocialShareCode { get; set; }

    public Guid PostId { get; set; }
    public Guid UserId { get; set; }
    public string? ShareNote { get; set; }
    public DateTime SharedAt { get; set; } = DateTime.UtcNow;

    public virtual SocialPost Post { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}