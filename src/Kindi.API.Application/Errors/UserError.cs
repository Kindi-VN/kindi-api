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

    /// <summary>Tạo tài khoản quản trị: tên đăng nhập đã có (kể cả tài khoản đã xoá mềm).</summary>
    public static Error UsernameAlreadyExists => new(ErrorStatus.Conflict, "User_UsernameAlreadyExists");

    /// <summary>Mật khẩu đặt tay quá ngắn.</summary>
    public static Error PasswordTooWeak => new(ErrorStatus.WrongRequest, "User_PasswordTooWeak");

    public static Error FullNameRequired => new(ErrorStatus.WrongRequest, "User_FullNameRequired");

    public static Error EmailRequired => new(ErrorStatus.WrongRequest, "User_EmailRequired");

    /// <summary>Vai trò không được gán qua API (SuperAdmin).</summary>
    public static Error RoleNotAssignable => new(ErrorStatus.WrongRequest, "User_RoleNotAssignable");

    /// <summary>Tài khoản SuperAdmin không sửa được từ màn quản lý người dùng.</summary>
    public static Error SuperAdminImmutable => new(ErrorStatus.Forbidden, "User_SuperAdminImmutable");
}
