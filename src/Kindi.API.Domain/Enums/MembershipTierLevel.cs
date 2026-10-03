namespace Kindi.API.Domain.Enums;

/// <summary>Thứ tự xét hạng thành viên khi doanh số tích luỹ chạm nhiều mốc (số lớn = hạng cao hơn).</summary>
public enum MembershipTierLevel
{
    /// <summary>Hạng khởi điểm — mọi tài khoản chưa đạt mốc nào.</summary>
    Starter = 1,

    /// <summary>Hạng bạc.</summary>
    Silver = 2,

    /// <summary>Hạng vàng.</summary>
    Gold = 3,

    /// <summary>Hạng bạch kim.</summary>
    Platinum = 4,

    /// <summary>Hạng kim cương.</summary>
    Diamond = 5
}
