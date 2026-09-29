namespace Kindi.API.Shared.Errors;

/// <summary>
/// Mã lỗi trả về cho client — luôn là chuỗi snake_case để UI bắt theo mã thay vì đoán theo HTTP status.
/// HTTP status tương ứng lấy bằng <see cref="ToHttpStatusCode"/>.
/// </summary>
public static class ErrorStatus
{
    /// <summary>Thành công.</summary>
    public const string Success = "success";

    /// <summary>Yêu cầu sai (vi phạm nghiệp vụ, thiếu dữ liệu bắt buộc...).</summary>
    public const string WrongRequest = "wrong_request";

    /// <summary>Dữ liệu gửi lên không qua kiểm tra hợp lệ.</summary>
    public const string ValidationError = "validation_error";

    public const string Unauthorized = "unauthorized";

    public const string Forbidden = "forbidden";

    public const string NotFound = "not_found";

    public const string Conflict = "conflict";

    /// <summary>Lỗi hệ thống (ngoài dự đoán) — message không trả chi tiết cho client.</summary>
    public const string SystemError = "system_error";

    /// <summary>HTTP status tương ứng với mã lỗi (mặc định 400).</summary>
    public static int ToHttpStatusCode(string status) => status switch
    {
        Unauthorized => 401,
        Forbidden => 403,
        NotFound => 404,
        Conflict => 409,
        SystemError => 500,
        _ => 400
    };
}
