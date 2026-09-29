using Kindi.API.Shared.Errors;

namespace Kindi.API.WebApi.Responses;

public class ApiResponse<T>
{
    public bool Success { get; set; }

    /// <summary>Mã trạng thái cho UI bắt: <see cref="ErrorStatus.Success"/> khi thành công, mã lỗi snake_case khi thất bại.</summary>
    public string? Status { get; set; }

    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }
    public DateTime Timestamp { get; set; }

    public ApiResponse()
    {
        Timestamp = DateTime.UtcNow;
    }

    public static ApiResponse<T> Ok(T data, string message = "Success")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Status = ErrorStatus.Success,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> Fail(string message, List<string>? errors = null, string status = ErrorStatus.WrongRequest)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Status = status,
            Message = message,
            Errors = errors
        };
    }
}
