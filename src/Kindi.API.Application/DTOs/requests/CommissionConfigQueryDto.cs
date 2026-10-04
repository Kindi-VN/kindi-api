namespace Kindi.API.Application.DTOs.requests;

using Kindi.API.Application.Common.Models;
using Kindi.API.Domain.Enums;

/// <summary>Điều kiện lọc danh sách cấu hình hoa hồng.</summary>
public class CommissionConfigQueryDto : PagedRequest
{
    /// <summary>Chỉ lấy cấu hình của một bên nhận hoa hồng.</summary>
    public CommissionBeneficiary? Beneficiary { get; set; }

    /// <summary>Tìm theo tên đăng nhập hoặc họ tên của tài khoản được áp riêng.</summary>
    public string? Search { get; set; }

    /// <summary>
    /// true = chỉ lấy cấu hình đã xoá mềm (bỏ qua global soft-delete filter).
    /// false/null = danh sách đang hoạt động như bình thường.
    /// </summary>
    public bool? IsDeleted { get; set; }
}
