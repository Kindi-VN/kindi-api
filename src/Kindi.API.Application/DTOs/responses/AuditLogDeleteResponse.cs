namespace Kindi.API.Application.DTOs.responses;

/// <summary>Kết quả xoá nhật ký hoạt động.</summary>
public class AuditLogDeleteResponse
{
    public AuditLogDeleteResponse()
    {
    }

    public AuditLogDeleteResponse(int deletedCount) => DeletedCount = deletedCount;

    /// <summary>Số dòng nhật ký đã xoá.</summary>
    public int DeletedCount { get; set; }
}
