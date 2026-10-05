namespace Kindi.API.Application.DTOs.requests;

/// <summary>Lọc danh sách phát sinh của một mã chia sẻ.</summary>
public class ReferralEventQueryDto
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    /// <summary>Lọc theo loại phát sinh.</summary>
    public Domain.Enums.ReferralEventType? EventType { get; set; }

    /// <summary>Từ khoá tìm kiếm phát sinh (mã chia sẻ, mã đối tượng, loại, trạng thái…).</summary>
    public string? Search { get; set; }

    /// <summary>
    /// Cột tìm kiếm tương ứng với <see cref="Search"/>; có giá trị thì CHỈ dò đúng cột đó.
    /// Bỏ trống thì giữ nguyên hành vi cũ (không lọc theo từ khoá).
    /// </summary>
    public Domain.Enums.ReferralEventSearchField? SearchField { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
