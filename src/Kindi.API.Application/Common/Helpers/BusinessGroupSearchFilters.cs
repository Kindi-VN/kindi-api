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
    /// Lọc theo đúng một trường khi client chỉ định. <paramref name="searchTerm"/> là từ khoá đã bỏ dấu + escape
    /// cho ILIKE; từ khoá rỗng hoặc không chỉ định trường thì trả về nguyên trạng.
    /// </summary>
    public static IQueryable<BusinessGroup> ApplyField(
        IQueryable<BusinessGroup> query, string? searchTerm, BusinessGroupSearchField? searchField)
    {
        if (string.IsNullOrEmpty(searchTerm) || searchField is null)
            return query;

        var pattern = "%" + searchTerm + "%";

        return searchField switch
        {
            BusinessGroupSearchField.Name => query.Where(x => EF.Functions.ILike(KindiDbFunctions.Unaccent(x.Name), pattern, "\\")),
            BusinessGroupSearchField.Description => query.Where(x => x.Description != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.Description), pattern, "\\")),
            BusinessGroupSearchField.Topic => query.Where(x => x.Topic != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.Topic), pattern, "\\")),
            BusinessGroupSearchField.BusinessFieldName => query.Where(x => x.BusinessFieldName != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.BusinessFieldName), pattern, "\\")),
            BusinessGroupSearchField.Code => query.Where(x => x.BusinessGroupCode != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.BusinessGroupCode), pattern, "\\")),
            _ => query
        };
    }
}
