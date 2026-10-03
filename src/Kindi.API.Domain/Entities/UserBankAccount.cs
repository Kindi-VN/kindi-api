// UserBankAccount.cs
namespace Kindi.API.Domain.Entities;

/// <summary>
/// Thông tin ngân hàng nhận giải ngân của một tài khoản (mỗi tài khoản một tài khoản ngân hàng).
/// Quản trị viên xác minh trước khi chi trả.
/// </summary>
public class UserBankAccount : BaseEntity
{
    public Guid UserId { get; set; }

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

    /// <summary>Ghi chú của quản trị viên khi xác minh.</summary>
    public string? Note { get; set; }

    /// <summary>Mã đối chiếu chuyển khoản dùng để xác minh tài khoản.</summary>
    public string? VerificationCode { get; set; }

    /// <summary>Thời điểm tạo mã đối chiếu.</summary>
    public DateTime? VerificationCodeIssuedAt { get; set; }

    // Navigation
    public virtual User User { get; set; } = null!;
}
