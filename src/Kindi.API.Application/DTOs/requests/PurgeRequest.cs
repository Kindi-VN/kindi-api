namespace Kindi.API.Application.DTOs.requests;

/// <summary>
/// Yêu cầu xoá VĨNH VIỄN các bản ghi đã xoá mềm: theo DANH SÁCH dòng được chọn (<see cref="Ids"/>)
/// hoặc theo KHOẢNG NGÀY xoá mềm (<see cref="FromDate"/>/<see cref="ToDate"/>). Không truyền điều kiện
/// nào thì API chặn để không xoá sạch dữ liệu.
/// </summary>
public class PurgeRequest
{
    /// <summary>Các bản ghi được tick chọn ở màn "Đã xoá".</summary>
    public List<Guid>? Ids { get; set; }

    /// <summary>Xoá từ ngày này trở đi (theo thời điểm xoá mềm — <c>UpdatedAt</c>).</summary>
    public DateTime? FromDate { get; set; }

    /// <summary>Xoá đến HẾT ngày này (theo thời điểm xoá mềm — <c>UpdatedAt</c>).</summary>
    public DateTime? ToDate { get; set; }
}
