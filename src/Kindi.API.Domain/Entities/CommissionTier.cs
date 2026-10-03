// CommissionTier.cs
namespace Kindi.API.Domain.Entities;

/// <summary>Một hạn mức của cấu hình hoa hồng.</summary>
public class CommissionTier : BaseEntity
{
    public Guid CommissionConfigId { get; set; }

    /// <summary>Giá trị bắt đầu của hạn mức (tính từ).</summary>
    public decimal FromValue { get; set; }

    /// <summary>Giá trị kết thúc của hạn mức; trống là không giới hạn.</summary>
    public decimal? ToValue { get; set; }

    /// <summary>Mức hoa hồng của hạn mức (% hoặc số tiền theo cấu hình).</summary>
    public decimal Rate { get; set; }

    // Navigation
    public virtual CommissionConfig CommissionConfig { get; set; } = null!;
}
