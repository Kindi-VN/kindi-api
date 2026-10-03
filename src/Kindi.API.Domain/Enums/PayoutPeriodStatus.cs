namespace Kindi.API.Domain.Enums;

/// <summary>Trạng thái kỳ giải ngân theo tháng.</summary>
public enum PayoutPeriodStatus
{
    /// <summary>Đang mở — còn ghi nhận hoa hồng phát sinh.</summary>
    Open = 1,

    /// <summary>Đã chốt sổ — không ghi nhận thêm, đã tạo danh sách chi trả chờ chuyển khoản.</summary>
    Closed = 2,

    /// <summary>Đã chi trả xong.</summary>
    Paid = 3
}
