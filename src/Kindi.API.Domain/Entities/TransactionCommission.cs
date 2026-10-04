// TransactionCommission.cs
using Kindi.API.Domain.Enums;

namespace Kindi.API.Domain.Entities;

/// <summary>
/// Hoa hồng của một bên nhận trong một bản khai doanh thu. Tỷ lệ được lưu lại tại đây nên sửa cấu hình
/// hoa hồng sau này không làm đổi số của bản khai cũ.
/// </summary>
public class TransactionCommission : BaseEntity
{
    /// <summary>Bản khai doanh thu mà hoa hồng này thuộc về.</summary>
    public Guid TransactionRevenueId { get; set; }

    /// <summary>Bản khai doanh thu.</summary>
    public TransactionRevenue TransactionRevenue { get; set; } = null!;

    /// <summary>Bên nhận hoa hồng.</summary>
    public CommissionBeneficiary Beneficiary { get; set; }

    /// <summary>Tài khoản nhận hoa hồng, để trống khi bên nhận chưa xác định được tài khoản.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Phát sinh hoa hồng tương ứng trong sổ hoa hồng, ghi khi chốt.</summary>
    public Guid? ReferralEventId { get; set; }

    /// <summary>Tỷ lệ hoa hồng (%) áp dụng cho bên này.</summary>
    public decimal Rate { get; set; }

    /// <summary>Số tiền hoa hồng.</summary>
    public decimal Amount { get; set; }
}
