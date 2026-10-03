namespace Kindi.API.Application.DTOs.requests;

using Kindi.API.Application.Common.Models;

/// <summary>Thông tin ngân hàng nhận giải ngân của chính thành viên.</summary>
public class SaveBankAccountRequest
{
    /// <summary>Tên ngân hàng.</summary>
    public string BankName { get; set; } = string.Empty;

    /// <summary>Chi nhánh.</summary>
    public string? Branch { get; set; }

    /// <summary>Số tài khoản.</summary>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>Chủ tài khoản.</summary>
    public string AccountHolder { get; set; } = string.Empty;
}

/// <summary>Điều kiện lọc danh sách thông tin ngân hàng.</summary>
public class BankAccountQueryDto : PagedRequest
{
    /// <summary>Tìm theo tên đăng nhập, họ tên, số tài khoản hoặc chủ tài khoản.</summary>
    public string? Search { get; set; }

    /// <summary>Lọc theo trạng thái xác minh.</summary>
    public bool? IsVerified { get; set; }
}

/// <summary>Kết quả xác minh thông tin ngân hàng của quản trị viên.</summary>
public class VerifyBankAccountRequest
{
    /// <summary>Đã xác minh hay chưa.</summary>
    public bool IsVerified { get; set; }

    /// <summary>Ghi chú khi xác minh.</summary>
    public string? Note { get; set; }
}
