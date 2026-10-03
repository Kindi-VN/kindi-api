namespace Kindi.API.Application.DTOs.responses;

/// <summary>Thông tin ngân hàng nhận giải ngân.</summary>
public class BankAccountResponse
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>Tên đăng nhập của chủ thông tin ngân hàng.</summary>
    public string? Username { get; set; }

    /// <summary>Họ tên của chủ thông tin ngân hàng.</summary>
    public string? FullName { get; set; }

    /// <summary>Tên ngân hàng.</summary>
    public string BankName { get; set; } = string.Empty;

    /// <summary>Chi nhánh.</summary>
    public string? Branch { get; set; }

    /// <summary>Số tài khoản.</summary>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>Chủ tài khoản.</summary>
    public string AccountHolder { get; set; } = string.Empty;

    /// <summary>Đã được quản trị viên xác minh.</summary>
    public bool IsVerified { get; set; }

    /// <summary>Thời điểm xác minh.</summary>
    public DateTime? VerifiedAt { get; set; }

    /// <summary>Người xác minh.</summary>
    public string? VerifiedBy { get; set; }

    /// <summary>Ghi chú khi xác minh.</summary>
    public string? Note { get; set; }

    /// <summary>Thời điểm cập nhật gần nhất.</summary>
    public DateTime UpdatedAt { get; set; }
}
