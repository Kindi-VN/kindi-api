using Kindi.API.Shared.Errors;

namespace Kindi.API.Application.Errors;

/// <summary>Lỗi nghiệp vụ của nhật ký hoạt động.</summary>
public static class AuditLogError
{
    public static Error NotFound => new(ErrorStatus.NotFound, "AuditLog_NotFound");

    /// <summary>Xoá nhật ký phải kèm điều kiện (chọn dòng hoặc khoảng ngày).</summary>
    public static Error DeleteCriteriaRequired => new(ErrorStatus.WrongRequest, "AuditLog_DeleteCriteriaRequired");

    /// <summary>Khoảng ngày xoá không hợp lệ (từ ngày lớn hơn đến ngày).</summary>
    public static Error DeleteRangeInvalid => new(ErrorStatus.WrongRequest, "AuditLog_DeleteRangeInvalid");

    /// <summary>Chọn quá nhiều dòng trong một lần xoá.</summary>
    public static Error TooManyIds => new(ErrorStatus.WrongRequest, "AuditLog_TooManyIds");
}
