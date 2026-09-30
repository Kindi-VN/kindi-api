using Kindi.API.Domain.Enums;

namespace Kindi.API.Application.DTOs.requests;

public class PurchaseRequestQueryDto
{
	public int Page { get; set; } = 1;
	public int PageSize { get; set; } = 20;
	public PurchaseRequestStatus? Status { get; set; }
	public string? Search { get; set; }
	public string? SortBy { get; set; }    // VD: "CreatedAt"
	public string? SortOrder { get; set; } // "asc" | "desc"
	public DateTime? FromDate { get; set; }
	public DateTime? ToDate { get; set; }

	/// <summary>
	/// Chỉ lấy yêu cầu của chính người gọi (khu vực thành viên). Người dùng không phải admin luôn bị giới hạn như vậy.
	/// </summary>
	public bool MineOnly { get; set; }
}