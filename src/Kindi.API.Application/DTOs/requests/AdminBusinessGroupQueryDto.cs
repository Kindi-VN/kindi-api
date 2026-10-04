using Kindi.API.Domain.Enums;

namespace Kindi.API.Application.DTOs.requests;

/// <summary>Query danh sách nhóm cho admin.</summary>
public class AdminBusinessGroupQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }

    /// <summary>
    /// Chỉ tìm theo đúng một trường (không OR lan sang trường khác); bỏ trống = tìm tập trường mặc định của danh sách.
    /// Giá trị hợp lệ: name, description, topic, businessFieldName, code.
    /// </summary>
    public BusinessGroupSearchField? SearchField { get; set; }

    public Guid? BusinessFieldId { get; set; }
    public bool? IsActive { get; set; }

    /// <summary>true = chỉ nhóm đang có yêu cầu vào nhóm chờ duyệt.</summary>
    public bool HasPendingMembers { get; set; }
    /// <summary>true = chỉ nhóm đang có yêu cầu kín gửi admin chờ xử lý.</summary>
    public bool HasPrivateRequests { get; set; }

    /// <summary>Lọc theo loại nhóm (Industry = nhóm ngành, Community = hội nhóm).</summary>
    public BusinessGroupType? Type { get; set; }

    /// <summary>Lọc theo trạng thái duyệt mở hội (dùng cho "hội nhóm chờ duyệt").</summary>
    public GroupApprovalStatus? ApprovalStatus { get; set; }

    /// <summary>
    /// true = chỉ lấy nhóm đã xoá mềm (bỏ qua global soft-delete filter).
    /// false/null = danh sách đang hoạt động như bình thường.
    /// </summary>
    public bool? IsDeleted { get; set; }
}
