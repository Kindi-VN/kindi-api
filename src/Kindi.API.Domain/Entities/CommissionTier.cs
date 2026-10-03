// CommissionTier.cs
namespace Kindi.API.Domain.Entities;

/// <summary>Một bậc của cấu hình hoa hồng theo bậc thang.</summary>
public class CommissionTier : BaseEntity
{
    public Guid CommissionConfigId { get; set; }

    /// <summary>Giá trị bắt đầu của bậc (tính từ).</summary>
    public decimal FromValue { get; set; }

    /// <summary>Giá trị kết thúc của bậc; trống là không giới hạn.</summary>
    public decimal? ToValue { get; set; }

    /// <summary>Mức hoa hồng của bậc (% hoặc số tiền theo cấu hình).</summary>
    public decimal Rate { get; set; }

    // Navigation
    public virtual CommissionConfig CommissionConfig { get; set; } = null!;
}
