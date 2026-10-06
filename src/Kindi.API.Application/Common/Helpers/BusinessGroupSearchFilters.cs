using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Kindi.API.Application.Common.Helpers;

/// <summary>
/// Lọc danh sách nhóm theo từ khoá. Khi client truyền <see cref="BusinessGroupSearchField"/> thì CHỈ dò
/// đúng một trường (tránh OR lan sang các cột khác); bỏ trống thì giữ nguyên tập trường mặc định của từng danh sách.
/// </summary>
public static class BusinessGroupSearchFilters
{
    /// <summary>Tên các trường hợp lệ cho tham số <c>searchField</c> (dùng cho tài liệu/UI).</summary>
    public static readonly IReadOnlyList<string> ValidFieldNames = new[]
    {
        "name",
        "description",
        "topic",
        "businessFieldName",
        "code"
    };

    /// <summary>
    /// Lọc theo đúng một trường khi client chỉ định. <paramref name="searchTerm"/> là từ khoá thô (Like() tự bỏ
    /// dấu và không phân biệt hoa/thường ở tầng DB); từ khoá rỗng hoặc không chỉ định trường thì trả về nguyên trạng.
    /// </summary>
    public static IQueryable<BusinessGroup> ApplyField(
        IQueryable<BusinessGroup> query, string? searchTerm, BusinessGroupSearchField? searchField)
    {
        if (string.IsNullOrEmpty(searchTerm) || searchField is null)
            return query;


        return searchField switch
        {
            BusinessGroupSearchField.Name => query.Where(x => x.Name.Like(searchTerm)),
            BusinessGroupSearchField.Description => query.Where(x => x.Description != null && x.Description.Like(searchTerm)),
            BusinessGroupSearchField.Topic => query.Where(x => x.Topic != null && x.Topic.Like(searchTerm)),
            BusinessGroupSearchField.BusinessFieldName => query.Where(x => x.BusinessFieldName != null && x.BusinessFieldName.Like(searchTerm)),
            BusinessGroupSearchField.Code => query.Where(x => x.BusinessGroupCode != null && x.BusinessGroupCode.EqualsCode(searchTerm)),
            _ => query
        };
    }
}
