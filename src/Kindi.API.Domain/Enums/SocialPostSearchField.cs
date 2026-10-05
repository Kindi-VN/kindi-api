namespace Kindi.API.Domain.Enums;

/// <summary>
/// Trường được tìm trong tham số <c>searchField</c> của danh sách bài viết social
/// (bảng tin cộng đồng và khu vực "Bài viết của tôi"). Client truyền đúng tên (không phân biệt
/// hoa/thường); bỏ trống thì tìm trên cả nội dung lẫn tác giả như mặc định.
/// </summary>
public enum SocialPostSearchField
{
    /// <summary>Nội dung bài viết.</summary>
    Content,

    /// <summary>Họ tên tác giả (bảng Users).</summary>
    AuthorFullName,

    /// <summary>Mã tài khoản tác giả (bảng Users).</summary>
    AuthorUserCode
}
