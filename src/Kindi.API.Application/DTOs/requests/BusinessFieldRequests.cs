namespace Kindi.API.Application.DTOs.Requests;

/// <summary>Tạo lĩnh vực hoạt động.</summary>
public class CreateBusinessFieldRequest
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Chuỗi JSON các tên gọi khác, ví dụ ["CNTT","IT"].</summary>
    public string? Aliases { get; set; }
}

/// <summary>Cập nhật lĩnh vực hoạt động.</summary>
public class UpdateBusinessFieldRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Aliases { get; set; }
    public bool IsActive { get; set; } = true;
}
