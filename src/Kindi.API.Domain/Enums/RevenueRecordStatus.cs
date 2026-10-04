namespace Kindi.API.Domain.Enums;

/// <summary>Trạng thái một bản khai doanh thu của giao dịch.</summary>
public enum RevenueRecordStatus
{
    /// <summary>Đang nhập, số liệu còn sửa được và chưa ghi vào sổ hoa hồng.</summary>
    Draft = 1,

    /// <summary>Đã chốt, số liệu được khoá để không lệch về sau.</summary>
    Confirmed = 2
}
