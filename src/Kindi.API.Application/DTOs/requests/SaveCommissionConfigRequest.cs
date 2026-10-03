namespace Kindi.API.Application.DTOs.requests;

using Kindi.API.Domain.Enums;

/// <summary>
/// Lưu cấu hình hoa hồng: <see cref="IsGlobal"/> = lưu bản chung cho mọi tài khoản, ngược lại lưu bản riêng
/// cho từng tài khoản trong <see cref="UserIds"/> (chọn một hoặc nhiều tài khoản cùng lúc).
/// </summary>
public class SaveCommissionConfigRequest
{
    /// <summary>Bên nhận hoa hồng.</summary>
    public CommissionBeneficiary Beneficiary { get; set; }

    /// <summary>true = bản chung cho mọi tài khoản.</summary>
    public bool IsGlobal { get; set; }

    /// <summary>Tài khoản được áp riêng; bỏ qua khi <see cref="IsGlobal"/>.</summary>
    public List<Guid> UserIds { get; set; } = new();

    /// <summary>Cách tính hoa hồng.</summary>
    public CommissionType Type { get; set; } = CommissionType.Percentage;

    /// <summary>Mức hoa hồng (% hoặc số tiền tuỳ <see cref="Type"/>).</summary>
    public decimal Rate { get; set; }

    /// <summary>Giá trị đơn tối thiểu.</summary>
    public decimal? MinOrderValue { get; set; }

    /// <summary>Trần hoa hồng của một phát sinh.</summary>
    public decimal? MaxCommission { get; set; }

    /// <summary>Đang áp dụng hay tạm dừng.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Ghi chú nội bộ.</summary>
    public string? Note { get; set; }

    /// <summary>Hạn mức; chỉ dùng khi <see cref="Type"/> là theo hạn mức.</summary>
    public List<CommissionTierRequest> Tiers { get; set; } = new();
}

/// <summary>Một hạn mức của cấu hình.</summary>
public class CommissionTierRequest
{
    /// <summary>Giá trị bắt đầu của hạn mức.</summary>
    public decimal FromValue { get; set; }

    /// <summary>Giá trị kết thúc; trống là không giới hạn.</summary>
    public decimal? ToValue { get; set; }

    /// <summary>Mức hoa hồng của hạn mức.</summary>
    public decimal Rate { get; set; }
}
