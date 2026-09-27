namespace Kindi.API.Domain.Entities;

public class PostTag : BaseEntity
{
    /// <summary>Mã liên kết bài viết - thẻ hiển thị cho người dùng.</summary>
    public string? PostTagCode { get; set; }

    public Guid PostId { get; set; }
    public virtual SocialPost Post { get; set; } = null!;

    public Guid TagId { get; set; }
    public virtual Tag Tag { get; set; } = null!;
}