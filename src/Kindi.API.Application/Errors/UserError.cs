using Kindi.API.Shared.Errors;

namespace Kindi.API.Application.Errors;

/// <summary>Lỗi nghiệp vụ của tài khoản người dùng.</summary>
public static class UserError
{
    public static Error WrongRequest => new(ErrorStatus.WrongRequest, "User_WrongRequest");

    public static Error NotFound => new(ErrorStatus.NotFound, "User_NotFound");

    public static Error PhoneRequired => new(ErrorStatus.WrongRequest, "User_PhoneRequired");

    public static Error PhoneAlreadyExists => new(ErrorStatus.Conflict, "User_PhoneAlreadyExists");

    public static Error EmailAlreadyExists => new(ErrorStatus.Conflict, "User_EmailAlreadyExists");
}
