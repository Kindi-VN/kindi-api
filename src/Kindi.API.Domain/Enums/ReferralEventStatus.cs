namespace Kindi.API.Domain.Enums;

/// <summary>Trạng thái đối soát của một phát sinh giới thiệu (khung sẵn cho tính năng hoa hồng).</summary>
public enum ReferralEventStatus
{
    /// <summary>Mới ghi nhận, chưa đối soát.</summary>
    Pending = 1,
    /// <summary>Hợp lệ, được tính hoa hồng.</summary>
    Approved = 2,
    /// <summary>Không hợp lệ (trùng, huỷ, tự giới thiệu…).</summary>
    Rejected = 3,
    /// <summary>Người được giới thiệu đã huỷ (rời đơn, rời nhóm…) — không tính vào thống kê.</summary>
    Cancelled = 5,
    /// <summary>Đã chi hoa hồng.</summary>
    Paid = 6
}
