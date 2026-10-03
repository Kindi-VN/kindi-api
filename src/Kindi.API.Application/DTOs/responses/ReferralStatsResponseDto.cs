namespace Kindi.API.Application.DTOs.responses;

/// <summary>Một dòng thống kê giới thiệu theo mã chia sẻ (màn quản trị).</summary>
public class ReferralStatsItemDto
{
    public string ReferralCode { get; set; } = string.Empty;

    /// <summary>Tên chủ mã (CTV hoặc tài khoản chia sẻ) — tra theo mã.</summary>
    public string? ReferrerName { get; set; }

    /// <summary>Số tài khoản được giới thiệu (đếm theo người, không theo lượt, không tính tài khoản khách).</summary>
    public int ReferredUsers { get; set; }

    /// <summary>Số tài khoản khách (tạo tự động khi gửi form) — tách riêng, không cộng vào số người được mời.</summary>
    public int ReferredGuestUsers { get; set; }

    /// <summary>Số phát sinh đã huỷ — đã trừ khỏi các cột thống kê phía trên.</summary>
    public int CancelledEvents { get; set; }

    /// <summary>Số đơn mua chung tạo từ mã này.</summary>
    public int GroupBuyingRequests { get; set; }

    /// <summary>Số lượt tham gia đơn mua chung.</summary>
    public int GroupBuyingJoins { get; set; }

    /// <summary>Số yêu cầu tìm nhà cung cấp.</summary>
    public int PurchaseRequests { get; set; }

    /// <summary>Số yêu cầu nhận offer.</summary>
    public int OfferRequests { get; set; }

    /// <summary>Số lượt xin vào nhóm ngành / hội nhóm.</summary>
    public int GroupMemberJoins { get; set; }

    /// <summary>Số đối tác đăng ký.</summary>
    public int PartnerRegisters { get; set; }

    /// <summary>Tổng số phát sinh.</summary>
    public int TotalEvents { get; set; }

    /// <summary>Tổng giá trị các phát sinh có ghi nhận giá trị.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Tổng hoa hồng đã tính (0 khi chưa cấu hình chính sách hoa hồng).</summary>
    public decimal TotalCommissionAmount { get; set; }

    /// <summary>Phát sinh gần nhất.</summary>
    public DateTime? LastEventAt { get; set; }
}

/// <summary>Số liệu tổng hợp hiển thị ở thẻ tổng quan.</summary>
public class ReferralStatsSummaryDto
{
    public int TotalReferrers { get; set; }
    public int TotalReferredGuestUsers { get; set; }
    public int TotalCancelledEvents { get; set; }
    public int TotalReferredUsers { get; set; }
    public int TotalEvents { get; set; }
    public int TotalGroupBuyingRequests { get; set; }
    public int TotalGroupBuyingJoins { get; set; }
    public int TotalPurchaseRequests { get; set; }
    public int TotalOfferRequests { get; set; }
    public int TotalGroupMemberJoins { get; set; }
    public int TotalPartnerRegisters { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalCommissionAmount { get; set; }
}

/// <summary>Số phát sinh theo ngày — dữ liệu cho biểu đồ.</summary>
public class ReferralStatsTimelineItemDto
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

/// <summary>Thẻ tổng quan + biểu đồ của board thống kê giới thiệu.</summary>
public class ReferralStatsOverviewDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public ReferralStatsSummaryDto Summary { get; set; } = new();
    public List<ReferralStatsTimelineItemDto> Timeline { get; set; } = new();
}

/// <summary>Một phát sinh giới thiệu (danh sách chi tiết của một mã chia sẻ).</summary>
public class ReferralEventResponseDto
{
    public Guid Id { get; set; }
    public string? ReferralEventCode { get; set; }
    public string ReferralCode { get; set; } = string.Empty;
    public string? ReferrerName { get; set; }
    public Guid ReferredUserId { get; set; }
    public string? ReferredUserName { get; set; }
    public Domain.Enums.ReferralEventType EventType { get; set; }
    public Guid? RefEntityId { get; set; }
    public string? RefEntityCode { get; set; }
    public decimal? Amount { get; set; }
    public decimal? CommissionAmount { get; set; }
    public Domain.Enums.ReferralEventStatus Status { get; set; }
    /// <summary>Phát sinh từ tài khoản khách (tạo tự động khi gửi form).</summary>
    public bool IsGuestAccount { get; set; }
    public DateTime CreatedAt { get; set; }
}
