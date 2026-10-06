namespace Kindi.API.Application.DTOs.responses;

/// <summary>Kết quả xoá vĩnh viễn bản ghi đã xoá mềm.</summary>
public class PurgeResponse
{
    public PurgeResponse()
    {
    }

    public PurgeResponse(string entityName, int deletedCount)
    {
        EntityName = entityName;
        DeletedCount = deletedCount;
    }

    /// <summary>Tên bảng/nghiệp vụ vừa xoá (để UI hiển thị rõ đã xoá ở đâu).</summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>Số bản ghi đã xoá vĩnh viễn.</summary>
    public int DeletedCount { get; set; }
}
