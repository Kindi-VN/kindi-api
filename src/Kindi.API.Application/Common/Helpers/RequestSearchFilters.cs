using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Kindi.API.Application.Common.Helpers;

/// <summary>
/// Lọc danh sách yêu cầu theo từ khoá. Khi client truyền <see cref="RequestSearchField"/> thì CHỈ dò
/// đúng một trường (tránh OR lan sang các cột khác gây truy vấn nặng và kết quả khó hiểu); bỏ trống thì
/// dò trên mọi trường như trước.
/// <para>
/// Bản ghi chỉ lưu MÃ người giới thiệu, không lưu tên — nên khi tìm theo TÊN người giới thiệu
/// (<see cref="RequestSearchField.RecordReferrerName"/> / <see cref="RequestSearchField.AccountReferrerName"/>),
/// caller phải quy tên về tập mã trước (xem <c>IReferralService.FindReferrerCodesByNameAsync</c>) rồi
/// truyền vào <c>referrerNameCodes</c>. Danh sách rỗng nghĩa là không khớp bản ghi nào.
/// </para>
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
        "customerEmail",
        "recordReferrerName",
        "accountReferrerName"
    };

    /// <summary>
    /// Lọc danh sách yêu cầu nhận offer. <paramref name="search"/> đã được trim; tìm theo kiểu chứa.
    /// <paramref name="referrerNameCodes"/> là tập mã chia sẻ của những người có tên khớp từ khoá
    /// (dùng cho <see cref="RequestSearchField.RecordReferrerName"/> / <see cref="RequestSearchField.AccountReferrerName"/>).
    /// </summary>
    public static IQueryable<OfferRequest> ApplyOffer(
        IQueryable<OfferRequest> query, string? search, RequestSearchField? searchField,
        IReadOnlyCollection<string>? referrerNameCodes = null)
    {
        if (string.IsNullOrEmpty(search))
            return query;

        var codes = referrerNameCodes ?? Array.Empty<string>();

        return searchField switch
        {
            null => query.Where(x =>
                x.ProductName.Contains(search) ||
                (x.User != null && x.User.FullName.Contains(search)) ||
                (x.User != null && x.User.Phone != null && x.User.Phone.Contains(search)) ||
                (x.User != null && x.User.Email != null && x.User.Email.Contains(search)) ||
                (x.OfferRequestCode != null && x.OfferRequestCode.Contains(search)) ||
                (x.RecordReferrerCode != null && codes.Contains(x.RecordReferrerCode!)) ||
                (x.User != null && x.User.AccountReferrerCode != null && codes.Contains(x.User.AccountReferrerCode!))),

            RequestSearchField.ProductName => query.Where(x => x.ProductName.Contains(search)),
            RequestSearchField.Code => query.Where(x => x.OfferRequestCode != null && x.OfferRequestCode.Contains(search)),
            RequestSearchField.RecordReferrerCode => query.Where(x => x.RecordReferrerCode != null && x.RecordReferrerCode.Contains(search)),
            RequestSearchField.RecordReferrerName => query.Where(x => x.RecordReferrerCode != null && codes.Contains(x.RecordReferrerCode!)),
            RequestSearchField.AccountReferrerName => query.Where(x => x.User != null && x.User.AccountReferrerCode != null && codes.Contains(x.User.AccountReferrerCode!)),
            RequestSearchField.CustomerName => query.Where(x => x.User != null && x.User.FullName.Contains(search)),
            RequestSearchField.CustomerPhone => query.Where(x => x.User != null && x.User.Phone != null && x.User.Phone.Contains(search)),
            RequestSearchField.CustomerEmail => query.Where(x => x.User != null && x.User.Email != null && x.User.Email.Contains(search)),
            _ => query
        };
    }

    /// <summary>
    /// Lọc danh sách yêu cầu mua. <paramref name="searchTerm"/> là từ khoá đã bỏ dấu + escape cho ILIKE.
    /// <paramref name="referrerNameCodes"/> là tập mã chia sẻ của những người có tên khớp từ khoá
    /// (dùng cho tên người giới thiệu).
    /// </summary>
    public static IQueryable<PurchaseRequest> ApplyPurchase(
        IQueryable<PurchaseRequest> query, string? searchTerm, RequestSearchField? searchField,
        IReadOnlyCollection<string>? referrerNameCodes = null)
    {
        if (string.IsNullOrEmpty(searchTerm))
            return query;

        var codes = referrerNameCodes ?? Array.Empty<string>();

        return searchField switch
        {
            null => query.Where(x =>
                (x.PurchaseRequestCode != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.PurchaseRequestCode), "%" + searchTerm + "%", "\\")) ||
                EF.Functions.ILike(KindiDbFunctions.Unaccent(x.ProductName), "%" + searchTerm + "%", "\\") ||
                (x.RecordReferrerCode != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.RecordReferrerCode), "%" + searchTerm + "%", "\\")) ||
                (x.User != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.User.FullName), "%" + searchTerm + "%", "\\")) ||
                (x.User != null && x.User.Phone != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.User.Phone), "%" + searchTerm + "%", "\\")) ||
                (x.User != null && x.User.Email != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.User.Email), "%" + searchTerm + "%", "\\")) ||
                (x.RecordReferrerCode != null && codes.Contains(x.RecordReferrerCode!)) ||
                (x.User != null && x.User.AccountReferrerCode != null && codes.Contains(x.User.AccountReferrerCode!))),

            RequestSearchField.ProductName => query.Where(x => EF.Functions.ILike(KindiDbFunctions.Unaccent(x.ProductName), "%" + searchTerm + "%", "\\")),
            RequestSearchField.Code => query.Where(x => x.PurchaseRequestCode != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.PurchaseRequestCode), "%" + searchTerm + "%", "\\")),
            RequestSearchField.RecordReferrerCode => query.Where(x => x.RecordReferrerCode != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.RecordReferrerCode), "%" + searchTerm + "%", "\\")),
            RequestSearchField.RecordReferrerName => query.Where(x => x.RecordReferrerCode != null && codes.Contains(x.RecordReferrerCode!)),
            RequestSearchField.AccountReferrerName => query.Where(x => x.User != null && x.User.AccountReferrerCode != null && codes.Contains(x.User.AccountReferrerCode!)),
            RequestSearchField.CustomerName => query.Where(x => x.User != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.User.FullName), "%" + searchTerm + "%", "\\")),
            RequestSearchField.CustomerPhone => query.Where(x => x.User != null && x.User.Phone != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.User.Phone), "%" + searchTerm + "%", "\\")),
            RequestSearchField.CustomerEmail => query.Where(x => x.User != null && x.User.Email != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.User.Email), "%" + searchTerm + "%", "\\")),
            _ => query
        };
    }

    /// <summary>
    /// Lọc danh sách yêu cầu mua chung. <paramref name="searchTerm"/> là từ khoá đã bỏ dấu + escape cho ILIKE.
    /// Người mở nhóm nằm ở bảng Users nên tên/SĐT/email lấy qua quan hệ <c>User</c>.
    /// <paramref name="referrerNameCodes"/> là tập mã chia sẻ của những người có tên khớp từ khoá
    /// (dùng cho tên người giới thiệu).
    /// </summary>
    public static IQueryable<GroupBuyingRequest> ApplyGroupBuying(
        IQueryable<GroupBuyingRequest> query, string? searchTerm, RequestSearchField? searchField,
        IReadOnlyCollection<string>? referrerNameCodes = null)
    {
        if (string.IsNullOrEmpty(searchTerm))
            return query;

        var codes = referrerNameCodes ?? Array.Empty<string>();

        return searchField switch
        {
            null => query.Where(x =>
                (x.GroupBuyingRequestCode != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.GroupBuyingRequestCode), "%" + searchTerm + "%", "\\")) ||
                EF.Functions.ILike(KindiDbFunctions.Unaccent(x.ProductName), "%" + searchTerm + "%", "\\") ||
                (x.RecordReferrerCode != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.RecordReferrerCode), "%" + searchTerm + "%", "\\")) ||
                EF.Functions.ILike(KindiDbFunctions.Unaccent(x.User.FullName), "%" + searchTerm + "%", "\\") ||
                (x.User.Phone != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.User.Phone), "%" + searchTerm + "%", "\\")) ||
                (x.User.Email != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.User.Email), "%" + searchTerm + "%", "\\")) ||
                (x.RecordReferrerCode != null && codes.Contains(x.RecordReferrerCode!)) ||
                (x.User.AccountReferrerCode != null && codes.Contains(x.User.AccountReferrerCode!))),

            RequestSearchField.ProductName => query.Where(x => EF.Functions.ILike(KindiDbFunctions.Unaccent(x.ProductName), "%" + searchTerm + "%", "\\")),
            RequestSearchField.Code => query.Where(x => x.GroupBuyingRequestCode != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.GroupBuyingRequestCode), "%" + searchTerm + "%", "\\")),
            RequestSearchField.RecordReferrerCode => query.Where(x => x.RecordReferrerCode != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.RecordReferrerCode), "%" + searchTerm + "%", "\\")),
            RequestSearchField.RecordReferrerName => query.Where(x => x.RecordReferrerCode != null && codes.Contains(x.RecordReferrerCode!)),
            RequestSearchField.AccountReferrerName => query.Where(x => x.User.AccountReferrerCode != null && codes.Contains(x.User.AccountReferrerCode!)),
            RequestSearchField.CustomerName => query.Where(x => EF.Functions.ILike(KindiDbFunctions.Unaccent(x.User.FullName), "%" + searchTerm + "%", "\\")),
            RequestSearchField.CustomerPhone => query.Where(x => x.User.Phone != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.User.Phone), "%" + searchTerm + "%", "\\")),
            RequestSearchField.CustomerEmail => query.Where(x => x.User.Email != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.User.Email), "%" + searchTerm + "%", "\\")),
            _ => query
        };
    }
}
