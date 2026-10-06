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
                x.ProductName.Like(search) ||
                (x.User != null && x.User.FullName.Like(search)) ||
                (x.User != null && x.User.Phone != null && x.User.Phone.Like(search)) ||
                (x.User != null && x.User.Email != null && x.User.Email.Like(search)) ||
                (x.OfferRequestCode != null && x.OfferRequestCode.EqualsCode(search)) ||
                (x.RecordReferrerCode != null && codes.Contains(x.RecordReferrerCode!)) ||
                (x.User != null && x.User.AccountReferrerCode != null && codes.Contains(x.User.AccountReferrerCode!))),

            RequestSearchField.ProductName => query.Where(x => x.ProductName.Like(search)),
            RequestSearchField.Code => query.Where(x => x.OfferRequestCode != null && x.OfferRequestCode.EqualsCode(search)),
            RequestSearchField.RecordReferrerCode => query.Where(x => x.RecordReferrerCode != null && x.RecordReferrerCode.EqualsCode(search)),
            RequestSearchField.RecordReferrerName => query.Where(x => x.RecordReferrerCode != null && codes.Contains(x.RecordReferrerCode!)),
            RequestSearchField.AccountReferrerName => query.Where(x => x.User != null && x.User.AccountReferrerCode != null && codes.Contains(x.User.AccountReferrerCode!)),
            RequestSearchField.CustomerName => query.Where(x => x.User != null && x.User.FullName.Like(search)),
            RequestSearchField.CustomerPhone => query.Where(x => x.User != null && x.User.Phone != null && x.User.Phone.Like(search)),
            RequestSearchField.CustomerEmail => query.Where(x => x.User != null && x.User.Email != null && x.User.Email.Like(search)),
            _ => query
        };
    }

    /// <summary>
    /// Lọc danh sách yêu cầu mua. <paramref name="searchTerm"/> là từ khoá thô (Like() tự bỏ dấu + không phân biệt hoa/thường).
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
                (x.PurchaseRequestCode != null && x.PurchaseRequestCode.EqualsCode(searchTerm)) ||
                x.ProductName.Like(searchTerm) ||
                (x.RecordReferrerCode != null && x.RecordReferrerCode.EqualsCode(searchTerm)) ||
                (x.User != null && x.User.FullName.Like(searchTerm)) ||
                (x.User != null && x.User.Phone != null && x.User.Phone.Like(searchTerm)) ||
                (x.User != null && x.User.Email != null && x.User.Email.Like(searchTerm)) ||
                (x.RecordReferrerCode != null && codes.Contains(x.RecordReferrerCode!)) ||
                (x.User != null && x.User.AccountReferrerCode != null && codes.Contains(x.User.AccountReferrerCode!))),

            RequestSearchField.ProductName => query.Where(x => x.ProductName.Like(searchTerm)),
            RequestSearchField.Code => query.Where(x => x.PurchaseRequestCode != null && x.PurchaseRequestCode.EqualsCode(searchTerm)),
            RequestSearchField.RecordReferrerCode => query.Where(x => x.RecordReferrerCode != null && x.RecordReferrerCode.EqualsCode(searchTerm)),
            RequestSearchField.RecordReferrerName => query.Where(x => x.RecordReferrerCode != null && codes.Contains(x.RecordReferrerCode!)),
            RequestSearchField.AccountReferrerName => query.Where(x => x.User != null && x.User.AccountReferrerCode != null && codes.Contains(x.User.AccountReferrerCode!)),
            RequestSearchField.CustomerName => query.Where(x => x.User != null && x.User.FullName.Like(searchTerm)),
            RequestSearchField.CustomerPhone => query.Where(x => x.User != null && x.User.Phone != null && x.User.Phone.Like(searchTerm)),
            RequestSearchField.CustomerEmail => query.Where(x => x.User != null && x.User.Email != null && x.User.Email.Like(searchTerm)),
            _ => query
        };
    }

    /// <summary>
    /// Lọc danh sách yêu cầu mua chung. <paramref name="searchTerm"/> là từ khoá thô (Like() tự bỏ dấu + không phân biệt hoa/thường).
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
                (x.GroupBuyingRequestCode != null && x.GroupBuyingRequestCode.EqualsCode(searchTerm)) ||
                x.ProductName.Like(searchTerm) ||
                (x.RecordReferrerCode != null && x.RecordReferrerCode.EqualsCode(searchTerm)) ||
                x.User.FullName.Like(searchTerm) ||
                (x.User.Phone != null && x.User.Phone.Like(searchTerm)) ||
                (x.User.Email != null && x.User.Email.Like(searchTerm)) ||
                (x.RecordReferrerCode != null && codes.Contains(x.RecordReferrerCode!)) ||
                (x.User.AccountReferrerCode != null && codes.Contains(x.User.AccountReferrerCode!))),

            RequestSearchField.ProductName => query.Where(x => x.ProductName.Like(searchTerm)),
            RequestSearchField.Code => query.Where(x => x.GroupBuyingRequestCode != null && x.GroupBuyingRequestCode.EqualsCode(searchTerm)),
            RequestSearchField.RecordReferrerCode => query.Where(x => x.RecordReferrerCode != null && x.RecordReferrerCode.EqualsCode(searchTerm)),
            RequestSearchField.RecordReferrerName => query.Where(x => x.RecordReferrerCode != null && codes.Contains(x.RecordReferrerCode!)),
            RequestSearchField.AccountReferrerName => query.Where(x => x.User.AccountReferrerCode != null && codes.Contains(x.User.AccountReferrerCode!)),
            RequestSearchField.CustomerName => query.Where(x => x.User.FullName.Like(searchTerm)),
            RequestSearchField.CustomerPhone => query.Where(x => x.User.Phone != null && x.User.Phone.Like(searchTerm)),
            RequestSearchField.CustomerEmail => query.Where(x => x.User.Email != null && x.User.Email.Like(searchTerm)),
            _ => query
        };
    }
}
