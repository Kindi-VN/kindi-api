namespace Kindi.API.Domain.Enums;

/// <summary>Kiểu chi trả hoa hồng.</summary>
public enum PayoutType
{
    /// <summary>Chi trả theo kỳ tháng (mặc định) — chốt sổ cuối tháng, chi trả đầu tháng sau.</summary>
    Monthly = 1,

    /// <summary>Rút sớm theo yêu cầu của thành viên — chịu phí rút sớm.</summary>
    Early = 2
}
