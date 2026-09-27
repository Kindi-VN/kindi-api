using System;

namespace Kindi.API.Domain.Entities;

public class SocialLike : BaseEntity
{
    /// <summary>Mã lượt thích bài viết hiển thị cho người dùng.</summary>
    public string? SocialLikeCode { get; set; }

    public Guid PostId { get; set; }
    public Guid UserId { get; set; }
    public DateTime LikedAt { get; set; } = DateTime.UtcNow;

    public virtual SocialPost Post { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}