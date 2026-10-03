namespace Kindi.API.Domain.Enums;

/// <summary>Trạng thái một lần chi trả hoa hồng.</summary>
public enum PayoutStatus
{
    /// <summary>Chờ xử lý (kỳ đã chốt, chờ chi trả; hoặc yêu cầu rút sớm chờ duyệt).</summary>
    Pending = 1,

    /// <summary>Đã duyệt, chờ chuyển khoản.</summary>
    Approved = 2,

    /// <summary>Từ chối chi trả.</summary>
    Rejected = 3,

    /// <summary>Đã chuyển khoản cho thành viên.</summary>
    Paid = 4,

    /// <summary>Thành viên huỷ yêu cầu rút sớm.</summary>
    Cancelled = 5
}
