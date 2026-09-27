namespace Kindi.API.Application.DTOs.requests;

/// <summary>
/// Admin chỉnh sửa thông tin yêu cầu mua chung (trường null = giữ nguyên giá trị cũ).
/// </summary>
public class UpdateGroupBuyingRequestDto
{
    public string? ProductName { get; set; }
    public string? ProductLink { get; set; }
    public int? TargetPeopleCount { get; set; }
    public decimal? TargetPrice { get; set; }
    public string? Note { get; set; }
}
