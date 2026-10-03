namespace Kindi.API.Application.DTOs.responses;

using Kindi.API.Domain.Enums;

/// <summary>Một hạng thành viên và quyền lợi kèm theo.</summary>
public class MembershipTierResponse
{
    public Guid Id { get; set; }

    /// <summary>Thứ tự hạng — số lớn là hạng cao hơn.</summary>
    public MembershipTierLevel Level { get; set; }

    /// <summary>Tên hạng.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Doanh số/hoa hồng tích luỹ tối thiểu để đạt hạng.</summary>
    public decimal MinAccumulatedValue { get; set; }

    /// <summary>Phí rút sớm riêng của hạng (%); trống là dùng mức chung.</summary>
    public decimal? EarlyWithdrawalFeeRate { get; set; }

    /// <summary>Hạn mức rút sớm trong tháng của hạng.</summary>
    public decimal? MonthlyWithdrawalLimit { get; set; }

    /// <summary>Thứ tự ưu tiên duyệt.</summary>
    public int ApprovalPriority { get; set; }

    /// <summary>Đang áp dụng hay tạm dừng.</summary>
    public bool IsActive { get; set; }

    /// <summary>Mô tả quyền lợi của hạng.</summary>
    public string? Description { get; set; }
}

/// <summary>Hạng thành viên hiện tại của chính người gọi.</summary>
public class MyMembershipResponse
{
    /// <summary>Hạng hiện tại; trống khi chưa xét được hạng.</summary>
    public MembershipTierResponse? Tier { get; set; }

    /// <summary>Doanh số/hoa hồng tích luỹ dùng để xét hạng.</summary>
    public decimal AccumulatedValue { get; set; }

    /// <summary>Doanh số cần thêm để lên hạng kế tiếp; trống khi đã ở hạng cao nhất.</summary>
    public decimal? NextTierRequirement { get; set; }

    /// <summary>Hạng kế tiếp.</summary>
    public MembershipTierResponse? NextTier { get; set; }

    /// <summary>Thời điểm xét hạng gần nhất.</summary>
    public DateTime? EvaluatedAt { get; set; }

    /// <summary>Phí rút sớm đang áp cho tài khoản (% — theo hạng nếu hạng có quy định riêng).</summary>
    public decimal EffectiveEarlyWithdrawalFeeRate { get; set; }

    /// <summary>Toàn bộ hạng đang áp dụng, xếp từ thấp tới cao — dùng để vẽ bảng quyền lợi theo hạng.</summary>
    public List<MembershipTierResponse> Tiers { get; set; } = new();
}
