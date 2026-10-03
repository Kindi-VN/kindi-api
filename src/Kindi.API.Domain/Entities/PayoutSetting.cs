// PayoutSetting.cs
namespace Kindi.API.Domain.Entities;

/// <summary>
/// Cấu hình phí rút sớm dùng chung: phí tính theo phần trăm số tiền rút, có mức tối thiểu/tối đa,
/// và số tiền rút tối thiểu. Hạng thành viên có thể ghi đè phí riêng.
/// </summary>
public class PayoutSetting : BaseEntity
{
    /// <summary>Cho phép thành viên gửi yêu cầu rút sớm.</summary>
    public bool IsEarlyWithdrawalEnabled { get; set; } = true;

    /// <summary>Phí rút sớm mặc định (% trên số tiền rút).</summary>
    public decimal EarlyWithdrawalFeeRate { get; set; }

    /// <summary>Phí rút sớm tối thiểu của một yêu cầu.</summary>
    public decimal? MinEarlyWithdrawalFee { get; set; }

    /// <summary>Phí rút sớm tối đa của một yêu cầu.</summary>
    public decimal? MaxEarlyWithdrawalFee { get; set; }

    /// <summary>Số tiền rút tối thiểu của một yêu cầu.</summary>
    public decimal MinWithdrawalAmount { get; set; }

    /// <summary>Ngày chốt sổ trong tháng (mặc định 0 = ngày cuối tháng).</summary>
    public int ClosingDay { get; set; }

    /// <summary>Ghi chú nội bộ.</summary>
    public string? Note { get; set; }
}
