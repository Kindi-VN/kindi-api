using Kindi.API.Domain.Enums;

namespace Kindi.API.Domain.Entities;

/// <summary>
/// Một phát sinh ghi nhận cho chủ mã chia sẻ (refcode): tài khoản được giới thiệu, đơn mua chung,
/// yêu cầu tìm hàng/offer, xin vào nhóm, đăng ký đối tác.
/// Các cột Amount/CommissionRate/CommissionAmount/Status là khung sẵn cho tính năng hoa hồng.
/// </summary>
public class ReferralEvent : BaseEntity
{
    public string? ReferralEventCode { get; set; }

    /// <summary>Mã chia sẻ ghi nhận (mã của CTV hoặc mã riêng của tài khoản).</summary>
    public string ReferralCode { get; set; } = string.Empty;

    /// <summary>UserId của chủ mã (null khi không xác định được chủ mã).</summary>
    public Guid? ReferrerUserId { get; set; }

    /// <summary>UserId của người được giới thiệu (người phát sinh thao tác).</summary>
    public Guid ReferredUserId { get; set; }

    public ReferralEventType EventType { get; set; }

    /// <summary>Id bản ghi phát sinh (đơn mua chung, yêu cầu, thành viên nhóm…).</summary>
    public Guid? RefEntityId { get; set; }

    /// <summary>Mã hiển thị của bản ghi phát sinh (GBR-…, PRQ-…) để tra cứu nhanh.</summary>
    public string? RefEntityCode { get; set; }

    /// <summary>Giá trị dùng để tính hoa hồng sau này (giá trị đơn / giá trị ước tính).</summary>
    public decimal? Amount { get; set; }

    /// <summary>Tỷ lệ hoa hồng áp dụng tại thời điểm ghi nhận (%).</summary>
    public decimal? CommissionRate { get; set; }

    /// <summary>Tiền hoa hồng tương ứng (null khi chưa cấu hình chính sách).</summary>
    public decimal? CommissionAmount { get; set; }

    /// <summary>
    /// Phát sinh từ tài khoản khách (tài khoản tạo tự động khi gửi form) — không tính vào số người được mời.
    /// </summary>
    public bool IsGuestAccount { get; set; }

    public ReferralEventStatus Status { get; set; } = ReferralEventStatus.Pending;

    public string? Note { get; set; }

    // Navigation
    public virtual User? Referrer { get; set; }
    public virtual User? Referred { get; set; }
}
