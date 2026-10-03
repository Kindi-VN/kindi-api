namespace Kindi.API.Application.DTOs.requests;

using Kindi.API.Domain.Enums;

/// <summary>Lưu một hạng thành viên (xét theo doanh số tích luỹ).</summary>
public class SaveMembershipTierRequest
{
    /// <summary>Thứ tự hạng — số lớn là hạng cao hơn.</summary>
    public MembershipTierLevel Level { get; set; }

    /// <summary>Tên hạng hiển thị cho thành viên.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Doanh số/hoa hồng tích luỹ tối thiểu để đạt hạng.</summary>
    public decimal MinAccumulatedValue { get; set; }

    /// <summary>Phí rút sớm riêng của hạng (%); trống là dùng mức chung.</summary>
    public decimal? EarlyWithdrawalFeeRate { get; set; }

    /// <summary>Hạn mức rút sớm trong tháng của hạng; trống là không giới hạn riêng.</summary>
    public decimal? MonthlyWithdrawalLimit { get; set; }

    /// <summary>Thứ tự ưu tiên duyệt — số nhỏ duyệt trước.</summary>
    public int ApprovalPriority { get; set; } = 100;

    /// <summary>Đang áp dụng hay tạm dừng.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Mô tả quyền lợi của hạng.</summary>
    public string? Description { get; set; }
}

/// <summary>Yêu cầu xét lại hạng thành viên.</summary>
public class EvaluateMembershipRequest
{
    /// <summary>Tài khoản cần xét lại; để trống là xét cho mọi tài khoản có phát sinh.</summary>
    public Guid? UserId { get; set; }
}
