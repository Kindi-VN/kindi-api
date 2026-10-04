namespace Kindi.API.Application.DTOs.requests;

public class GetGroupBuyingRequestsQueryDto
{
	public int Page { get; set; } = 1;
	public int PageSize { get; set; } = 20;
	public string? Status { get; set; }
	public string? Search { get; set; }
	public string? SortBy { get; set; }    // VD: "CreatedAt"
	public string? SortOrder { get; set; } // "asc" | "desc"

	/// <summary>
	/// true = chỉ lấy yêu cầu đã xoá mềm (bỏ qua global soft-delete filter).
	/// false/null = danh sách đang hoạt động như bình thường.
	/// </summary>
	public bool? IsDeleted { get; set; }
}