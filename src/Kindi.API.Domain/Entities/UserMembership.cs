// UserMembership.cs
namespace Kindi.API.Domain.Entities;

/// <summary>
/// Hạng thành viên hiện tại của một tài khoản, kèm doanh số tích luỹ đã dùng để xét hạng.
/// Bản ghi được tính lại khi có phát sinh mới hoặc khi quản trị viên yêu cầu xét lại.
/// </summary>
public class UserMembership : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid MembershipTierId { get; set; }

    /// <summary>Doanh số/hoa hồng tích luỹ tại thời điểm xét hạng.</summary>
    public decimal AccumulatedValue { get; set; }

    /// <summary>Thời điểm xét hạng gần nhất.</summary>
    public DateTime EvaluatedAt { get; set; }

    // Navigation
    public virtual User User { get; set; } = null!;
    public virtual MembershipTier MembershipTier { get; set; } = null!;
}
