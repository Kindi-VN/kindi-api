using Kindi.API.Domain.Enums;

namespace Kindi.API.Application.DTOs.requests;

/// <summary>Query danh sách nhóm (công khai) — Page/PageSize theo convention chung.</summary>
public class BusinessGroupQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public string? Search { get; set; }
    public Guid? BusinessFieldId { get; set; }

    /// <summary>true = chỉ nhóm mà người đang đăng nhập đã là thành viên.</summary>
    public bool MineOnly { get; set; }

    /// <summary>Danh sách "nhóm của tôi": created = nhóm mình tạo, joined = nhóm mình đã tham gia, bỏ trống = cả hai.</summary>
    public GroupMineRole? MineRole { get; set; }
}
