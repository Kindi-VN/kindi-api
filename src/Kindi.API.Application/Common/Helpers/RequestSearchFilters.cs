using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Kindi.API.Application.Common.Helpers;

/// <summary>
/// Lọc danh sách yêu cầu theo từ khoá. Khi client truyền <see cref="RequestSearchField"/> thì CHỈ dò
/// đúng một trường (tránh OR lan sang các cột khác gây truy vấn nặng và kết quả khó hiểu); bỏ trống thì
/// dò trên mọi trường như trước.
/// </summary>
public static class RequestSearchFilters
{
    /// <summary>Tên các trường hợp lệ cho tham số <c>searchField</c> (dùng cho tài liệu/UI).</summary>
    public static readonly IReadOnlyList<string> ValidFieldNames = new[]
    {
        "productName",
        "code",
        "recordReferrerCode",
        "customerName",
        "customerPhone",
        "customerEmail"
    };

    /// <summary>
    /// Lọc danh sách yêu cầu nhận offer. <paramref name="search"/> đã được trim; tìm theo kiểu chứa.
    /// </summary>
    public static IQueryable<OfferRequest> ApplyOffer(
        IQueryable<OfferRequest> query, string? search, RequestSearchField? searchField)
    {
        if (string.IsNullOrEmpty(search))
            return query;

        return searchField switch
        {
            null => query.Where(x =>
                x.ProductName.Contains(search) ||
                (x.User != null && x.User.FullName.Contains(search)) ||
                (x.User != null && x.User.Phone != null && x.User.Phone.Contains(search)) ||
                (x.User != null && x.User.Email != null && x.User.Email.Contains(search)) ||
                (x.OfferRequestCode != null && x.OfferRequestCode.Contains(search))),

            RequestSearchField.ProductName => query.Where(x => x.ProductName.Contains(search)),
            RequestSearchField.Code => query.Where(x => x.OfferRequestCode != null && x.OfferRequestCode.Contains(search)),
            RequestSearchField.RecordReferrerCode => query.Where(x => x.RecordReferrerCode != null && x.RecordReferrerCode.Contains(search)),
            RequestSearchField.CustomerName => query.Where(x => x.User != null && x.User.FullName.Contains(search)),
            RequestSearchField.CustomerPhone => query.Where(x => x.User != null && x.User.Phone != null && x.User.Phone.Contains(search)),
            RequestSearchField.CustomerEmail => query.Where(x => x.User != null && x.User.Email != null && x.User.Email.Contains(search)),
            _ => query
        };
    }

    /// <summary>
    /// Lọc danh sách yêu cầu mua. <paramref name="searchTerm"/> là từ khoá đã bỏ dấu + escape cho ILIKE.
    /// </summary>
    public static IQueryable<PurchaseRequest> ApplyPurchase(
        IQueryable<PurchaseRequest> query, string? searchTerm, RequestSearchField? searchField)
    {
        if (string.IsNullOrEmpty(searchTerm))
            return query;

        return searchField switch
        {
            null => query.Where(x =>
                (x.PurchaseRequestCode != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.PurchaseRequestCode), "%" + searchTerm + "%", "\\")) ||
                EF.Functions.ILike(KindiDbFunctions.Unaccent(x.ProductName), "%" + searchTerm + "%", "\\") ||
                (x.RecordReferrerCode != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.RecordReferrerCode), "%" + searchTerm + "%", "\\")) ||
                (x.User != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.User.FullName), "%" + searchTerm + "%", "\\")) ||
                (x.User != null && x.User.Phone != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.User.Phone), "%" + searchTerm + "%", "\\")) ||
                (x.User != null && x.User.Email != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.User.Email), "%" + searchTerm + "%", "\\"))),

            RequestSearchField.ProductName => query.Where(x => EF.Functions.ILike(KindiDbFunctions.Unaccent(x.ProductName), "%" + searchTerm + "%", "\\")),
            RequestSearchField.Code => query.Where(x => x.PurchaseRequestCode != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.PurchaseRequestCode), "%" + searchTerm + "%", "\\")),
            RequestSearchField.RecordReferrerCode => query.Where(x => x.RecordReferrerCode != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.RecordReferrerCode), "%" + searchTerm + "%", "\\")),
            RequestSearchField.CustomerName => query.Where(x => x.User != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.User.FullName), "%" + searchTerm + "%", "\\")),
            RequestSearchField.CustomerPhone => query.Where(x => x.User != null && x.User.Phone != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.User.Phone), "%" + searchTerm + "%", "\\")),
            RequestSearchField.CustomerEmail => query.Where(x => x.User != null && x.User.Email != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.User.Email), "%" + searchTerm + "%", "\\")),
            _ => query
        };
    }
}
