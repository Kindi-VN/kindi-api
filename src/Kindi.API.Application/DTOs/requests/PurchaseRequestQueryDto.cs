using Kindi.API.Domain.Enums;

namespace Kindi.API.Application.DTOs.requests;

public class PurchaseRequestQueryDto
{
	public int Page { get; set; } = 1;
	public int PageSize { get; set; } = 20;
	public PurchaseRequestStatus? Status { get; set; }
	public string? Search { get; set; }

	/// <summary>
	/// Chỉ tìm theo đúng một trường (không OR lan sang trường khác); bỏ trống = tìm mọi trường như trước.
	/// Giá trị hợp lệ: productName, code, recordReferrerCode, customerName, customerPhone, customerEmail.
	/// </summary>
	public RequestSearchField? SearchField { get; set; }

	public string? SortBy { get; set; }    // VD: "CreatedAt"
	public string? SortOrder { get; set; } // "asc" | "desc"
	public DateTime? FromDate { get; set; }
	public DateTime? ToDate { get; set; }

	/// <summary>
	/// Chỉ lấy yêu cầu của chính người gọi (khu vực thành viên). Người dùng không phải admin luôn bị giới hạn như vậy.
	/// </summary>
	public bool MineOnly { get; set; }

	/// <summary>Lọc danh sách đã xoá mềm (soft-delete, admin xem tab "Đã xoá").</summary>
	public bool? IsDeleted { get; set; }
}