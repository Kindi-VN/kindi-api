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
}
