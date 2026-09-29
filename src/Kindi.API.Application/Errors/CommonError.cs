using Kindi.API.Shared.Errors;

namespace Kindi.API.Application.Errors;

/// <summary>Lỗi dùng chung cho mọi module.</summary>
public static class CommonError
{
    public static Error WrongRequest => new(ErrorStatus.WrongRequest, "Common_WrongRequest");

    public static Error NotFound => new(ErrorStatus.NotFound, "Common_NotFound");

    public static Error Unauthorized => new(ErrorStatus.Unauthorized, "Common_Unauthorized");

    public static Error Forbidden => new(ErrorStatus.Forbidden, "Common_Forbidden");

    public static Error Conflict => new(ErrorStatus.Conflict, "Common_Conflict");
}
