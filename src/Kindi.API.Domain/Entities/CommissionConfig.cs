// CommissionConfig.cs
using Kindi.API.Domain.Enums;

namespace Kindi.API.Domain.Entities;

/// <summary>
/// Cấu hình mức hoa hồng: <see cref="UserId"/> trống là bản chung cho mọi tài khoản, có giá trị là bản riêng
/// của tài khoản đó. Bản riêng luôn được ưu tiên hơn bản chung khi xác định mức áp dụng.
/// </summary>
public class CommissionConfig : BaseEntity
{
    /// <summary>Bên nhận hoa hồng.</summary>
    public CommissionBeneficiary Beneficiary { get; set; }

    /// <summary>Tài khoản được áp riêng; trống là bản chung.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Cách tính: phần trăm, số tiền cố định hay bậc thang.</summary>
    public CommissionType Type { get; set; }

    /// <summary>Mức hoa hồng (% khi <see cref="Type"/> là phần trăm, số tiền khi là cố định).</summary>
    public decimal Rate { get; set; }

    /// <summary>Giá trị đơn tối thiểu để được tính hoa hồng.</summary>
    public decimal? MinOrderValue { get; set; }

    /// <summary>Trần hoa hồng của một phát sinh.</summary>
    public decimal? MaxCommission { get; set; }

    /// <summary>Đang áp dụng hay tạm dừng.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Ghi chú nội bộ.</summary>
    public string? Note { get; set; }

    // Navigation
    public virtual User? User { get; set; }
    public virtual ICollection<CommissionTier> Tiers { get; set; } = new List<CommissionTier>();
}
