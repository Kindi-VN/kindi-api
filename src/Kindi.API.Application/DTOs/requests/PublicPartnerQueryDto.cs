using Kindi.API.Domain.Enums;

namespace Kindi.API.Application.DTOs.Requests;

/// <summary>
/// Query danh sách nhà cung cấp CÔNG KHAI (trang Nguồn cung) — chỉ trả về đối tác doanh nghiệp
/// đã được duyệt/đang hoạt động.
/// </summary>
public class PublicPartnerQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public string? Search { get; set; }
    public Guid? BusinessFieldId { get; set; }

    /// <summary>
    /// Chỉ tìm theo đúng một trường (không OR lan sang cột khác); bỏ trống = tìm nhiều trường như trước.
    /// Dùng chung <see cref="PartnerSearchField"/> với danh sách đối tác ở màn quản trị.
    /// </summary>
    public PartnerSearchField? SearchField { get; set; }
}
