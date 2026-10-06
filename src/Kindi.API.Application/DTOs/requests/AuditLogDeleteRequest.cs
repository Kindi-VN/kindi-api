namespace Kindi.API.Application.DTOs.requests;

/// <summary>
/// Yêu cầu xoá nhật ký hoạt động: xoá theo DANH SÁCH dòng được chọn (<see cref="Ids"/>) HOẶC theo
/// KHOẢNG NGÀY (<see cref="FromDate"/>/<see cref="ToDate"/>). Không truyền điều kiện nào thì API chặn
/// để không xoá sạch nhật ký.
/// </summary>
public class AuditLogDeleteRequest
{
    /// <summary>Các dòng nhật ký được tick chọn trên màn hình.</summary>
    public List<Guid>? Ids { get; set; }

    /// <summary>Xoá từ ngày này trở đi (dạng YYYY-MM-DD).</summary>
    public DateTime? FromDate { get; set; }

    /// <summary>Xoá đến HẾT ngày này (dạng YYYY-MM-DD).</summary>
    public DateTime? ToDate { get; set; }
}
