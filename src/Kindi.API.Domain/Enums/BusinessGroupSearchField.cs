namespace Kindi.API.Domain.Enums;

/// <summary>
/// Trường được tìm trong tham số <c>searchField</c> của các danh sách nhóm.
/// Client truyền đúng tên (không phân biệt hoa/thường); bỏ trống thì tìm trên tập trường mặc định của từng danh sách.
/// </summary>
public enum BusinessGroupSearchField
{
    /// <summary>Tên nhóm.</summary>
    Name,

    /// <summary>Mô tả nhóm.</summary>
    Description,

    /// <summary>Chủ đề của hội nhóm.</summary>
    Topic,

    /// <summary>Tên lĩnh vực kinh doanh của nhóm ngành.</summary>
    BusinessFieldName,

    /// <summary>Mã hiển thị của nhóm (GRP-…).</summary>
    Code
}
