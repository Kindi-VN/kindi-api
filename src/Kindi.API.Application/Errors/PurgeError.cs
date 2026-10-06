using Kindi.API.Shared.Errors;

namespace Kindi.API.Application.Errors;

/// <summary>Lỗi nghiệp vụ của thao tác xoá vĩnh viễn ở màn "Đã xoá".</summary>
public static class PurgeError
{
    /// <summary>Xoá vĩnh viễn phải kèm điều kiện (chọn dòng hoặc khoảng ngày).</summary>
    public static Error CriteriaRequired => new(ErrorStatus.WrongRequest, "Purge_CriteriaRequired");

    /// <summary>Khoảng ngày không hợp lệ (từ ngày lớn hơn đến ngày).</summary>
    public static Error RangeInvalid => new(ErrorStatus.WrongRequest, "Purge_RangeInvalid");

    /// <summary>Chọn quá nhiều dòng trong một lần xoá.</summary>
    public static Error TooManyIds => new(ErrorStatus.WrongRequest, "Purge_TooManyIds");

    /// <summary>Bản ghi còn dữ liệu con tham chiếu nên không xoá vĩnh viễn được (khoá ngoại).</summary>
    public static Error HasRelatedData => new(ErrorStatus.WrongRequest, "Purge_HasRelatedData");
}
