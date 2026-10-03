// MembershipTier.cs
using Kindi.API.Domain.Enums;

namespace Kindi.API.Domain.Entities;

/// <summary>
/// Hạng thành viên: xét tự động theo doanh số/hoa hồng tích luỹ (mua chung, offer, người giới thiệu).
/// Hạng mang quyền lợi: mức hoa hồng riêng (cấu hình ở <see cref="CommissionConfig"/> theo hạng),
/// phí rút sớm thấp hơn, hạn mức rút trong tháng và thứ tự ưu tiên duyệt.
/// </summary>
public class MembershipTier : BaseEntity
{
    /// <summary>Tên hạng hiển thị cho thành viên.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Thứ tự hạng — số lớn là hạng cao hơn, dùng khi một tài khoản chạm nhiều mốc.</summary>
    public MembershipTierLevel Level { get; set; }

    /// <summary>Doanh số/hoa hồng tích luỹ tối thiểu để đạt hạng.</summary>
    public decimal MinAccumulatedValue { get; set; }

    /// <summary>Phí rút sớm riêng của hạng (% trên số tiền rút); trống là dùng mức chung.</summary>
    public decimal? EarlyWithdrawalFeeRate { get; set; }

    /// <summary>Hạn mức rút sớm trong một tháng của hạng; trống là không giới hạn riêng.</summary>
    public decimal? MonthlyWithdrawalLimit { get; set; }

    /// <summary>Thứ tự ưu tiên duyệt — số nhỏ được duyệt trước.</summary>
    public int ApprovalPriority { get; set; } = 100;

    /// <summary>Đang áp dụng hay tạm dừng.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Mô tả quyền lợi của hạng (hiển thị cho thành viên).</summary>
    public string? Description { get; set; }
}
