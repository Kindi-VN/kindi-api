namespace Kindi.API.Application.Common.Extensions;

/// <summary>
/// Tìm kiếm chuỗi NGAY TRONG truy vấn EF, viết gọn ở chỗ gọi:
/// <code>x.ProductName.Like(keyword)</code>
/// thay cho <c>EF.Functions.ILike(KindiDbFunctions.Unaccent(x.ProductName), "%" + keyword + "%", "\\")</c>
/// lặp đi lặp lại ở mọi trường tìm kiếm.
/// <para>
/// Ở tầng DB, <see cref="Like"/> được dịch thành <c>strpos(lower(unaccent(col)), lower(unaccent(từ khoá))) &gt; 0</c>
/// — bỏ dấu tiếng Việt cả hai vế, không phân biệt hoa/thường, và từ khoá được hiểu NGUYÊN VĂN nên KHÔNG cần
/// escape <c>%</c>/<c>_</c> ở chỗ gọi (xem <c>KindiModelBuilderExtensions.ConfigureKindiModel</c>).
/// </para>
/// <para>
/// MÃ (code) thì KHÔNG dùng <see cref="Like"/>: mã là duy nhất và có index, dùng <see cref="EqualsCode"/>
/// để câu truy vấn chỉ so bằng <c>=</c> và còn dùng được index.
/// </para>
/// </summary>
public static class StringQueryExtensions
{
    /// <summary>Ký tự thoát cho <c>%</c> và <c>_</c> của phép LIKE — chỉ còn dùng cho câu truy vấn cũ.</summary>
    public const string LikeEscape = "\\";

    /// <summary>
    /// Tìm CHỨA từ khoá, bỏ dấu tiếng Việt và không phân biệt hoa/thường — dùng cho tên, mô tả, nội dung.
    /// Ngoài truy vấn EF (ví dụ test in-memory) chạy bằng so khớp chuỗi con đã bỏ dấu.
    /// </summary>
    public static bool Like(this string? value, string? keyword)
        => keyword is null
            || (value is not null && value.RemoveVietnameseSign()
                .Contains(keyword.RemoveVietnameseSign(), StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// So khớp ĐÚNG giá trị của một cột MÃ: từ khoá được chuẩn hoá hoa/thường ngay ở tham số nên cột giữ
    /// nguyên và index vẫn dùng được. Mã là duy nhất nên tìm một phần phải dùng <see cref="Like"/>.
    /// </summary>
    public static bool EqualsCode(this string? value, string? code)
        => value == code?.Trim().ToUpperInvariant();

    /// <summary>
    /// So khớp ĐÚNG một giá trị chữ (không tìm một phần) mà vẫn bỏ dấu tiếng Việt + không phân biệt hoa/thường,
    /// ví dụ kiểm tra trùng tên công ty. Phải bỏ dấu cả hai vế nên câu SQL không dùng được index — chỉ dùng
    /// cho câu kiểm tra duy nhất, không dùng cho danh sách.
    /// </summary>
    public static bool SameText(this string? value, string? text)
        => value is not null
            && string.Equals(value.RemoveVietnameseSign(), text?.Trim().RemoveVietnameseSign(),
                StringComparison.OrdinalIgnoreCase);
}
