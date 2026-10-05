namespace Kindi.API.Domain.Enums;

/// <summary>
/// Trường được tìm trong tham số <c>searchField</c> của danh sách phát sinh giới thiệu.
/// Client truyền đúng tên (không phân biệt hoa/thường); bỏ trống thì giữ nguyên hành vi cũ (không lọc theo từ khoá).
/// </summary>
public enum ReferralEventSearchField
{
    /// <summary>Mã chia sẻ ghi nhận của phát sinh (cột RecordReferrerCode).</summary>
    ReferralCode,

    /// <summary>Mã đối tượng/đơn được ghi nhận trên phát sinh (cột RefEntityCode, ví dụ GBR-… / PRQ-…).</summary>
    RefEntityCode,

    /// <summary>Loại phát sinh (cột EventType).</summary>
    EventType,

    /// <summary>Trạng thái đối soát của phát sinh (cột Status).</summary>
    Status
}
