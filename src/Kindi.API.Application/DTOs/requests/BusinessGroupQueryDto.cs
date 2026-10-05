using Kindi.API.Domain.Enums;

namespace Kindi.API.Application.DTOs.requests;

/// <summary>Query danh sách nhóm (công khai) — Page/PageSize theo convention chung.</summary>
public class BusinessGroupQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public string? Search { get; set; }

    /// <summary>
    /// Chỉ tìm theo đúng một trường (không OR lan sang trường khác); bỏ trống = tìm tập trường mặc định của danh sách.
    /// Giá trị hợp lệ: name, description, topic, businessFieldName, code.
    /// </summary>
    public BusinessGroupSearchField? SearchField { get; set; }

    public Guid? BusinessFieldId { get; set; }

    /// <summary>true = chỉ nhóm mà người đang đăng nhập đã là thành viên.</summary>
    public bool MineOnly { get; set; }

    /// <summary>Danh sách "nhóm của tôi": created = nhóm mình tạo, joined = nhóm mình đã tham gia, bỏ trống = cả hai.</summary>
    public GroupMineRole? MineRole { get; set; }

    /// <summary>
    /// Chỉ lấy nhóm tạo từ ngày này trở đi (theo CreatedAt); bỏ trống = không giới hạn (giữ nguyên hành vi cũ).
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Chỉ lấy nhóm tạo đến hết ngày này (bao gồm cả ngày này, theo CreatedAt); bỏ trống = không giới hạn.
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Lọc theo trạng thái duyệt của nhóm (Pending/Approved/Rejected); bỏ trống = mọi trạng thái.
    /// Dùng cho danh sách "nhóm của tôi".
    /// </summary>
    public GroupApprovalStatus? ApprovalStatus { get; set; }
}
