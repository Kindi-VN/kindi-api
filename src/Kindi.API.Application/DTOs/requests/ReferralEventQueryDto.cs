namespace Kindi.API.Application.DTOs.requests;

/// <summary>Lọc danh sách phát sinh của một mã chia sẻ.</summary>
public class ReferralEventQueryDto
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    /// <summary>Lọc theo loại phát sinh.</summary>
    public Domain.Enums.ReferralEventType? EventType { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
