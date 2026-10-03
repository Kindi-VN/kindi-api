namespace Kindi.API.Application.DTOs.responses;

using Kindi.API.Domain.Enums;

/// <summary>Cấu hình hoa hồng trả về màn quản trị và màn "hoa hồng của tôi".</summary>
public class CommissionConfigResponse
{
    public Guid Id { get; set; }

    /// <summary>Bên nhận hoa hồng.</summary>
    public CommissionBeneficiary Beneficiary { get; set; }

    /// <summary>Tài khoản được áp riêng; trống là bản chung.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Họ tên tài khoản được áp riêng.</summary>
    public string? UserFullName { get; set; }

    /// <summary>Tên đăng nhập của tài khoản được áp riêng.</summary>
    public string? Username { get; set; }

    /// <summary>Bản chung cho mọi tài khoản.</summary>
    public bool IsGlobal => UserId == null;

    /// <summary>Bản riêng của một tài khoản.</summary>
    public bool IsPersonal => UserId != null;

    /// <summary>Cách tính hoa hồng.</summary>
    public CommissionType Type { get; set; }

    /// <summary>Mức hoa hồng.</summary>
    public decimal Rate { get; set; }

    /// <summary>Giá trị đơn tối thiểu.</summary>
    public decimal? MinOrderValue { get; set; }

    /// <summary>Trần hoa hồng của một phát sinh.</summary>
    public decimal? MaxCommission { get; set; }

    /// <summary>Đang áp dụng hay tạm dừng.</summary>
    public bool IsActive { get; set; }

    /// <summary>Ghi chú nội bộ.</summary>
    public string? Note { get; set; }

    /// <summary>Hạn mức của cấu hình.</summary>
    public List<CommissionTierResponse> Tiers { get; set; } = new();

    /// <summary>Thời điểm cập nhật gần nhất.</summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Một hạn mức của cấu hình hoa hồng.</summary>
public class CommissionTierResponse
{
    public Guid Id { get; set; }

    /// <summary>Giá trị bắt đầu của hạn mức.</summary>
    public decimal FromValue { get; set; }

    /// <summary>Giá trị kết thúc; trống là không giới hạn.</summary>
    public decimal? ToValue { get; set; }

    /// <summary>Mức hoa hồng của hạn mức.</summary>
    public decimal Rate { get; set; }
}

/// <summary>Mức hoa hồng đang áp cho chính người gọi: bản riêng nếu có, không thì bản chung.</summary>
public class MyCommissionResponse
{
    /// <summary>Mức áp khi người gọi là người giới thiệu.</summary>
    public CommissionConfigResponse? Referrer { get; set; }

    /// <summary>Mức áp khi người gọi là đối tác.</summary>
    public CommissionConfigResponse? Partner { get; set; }
}
