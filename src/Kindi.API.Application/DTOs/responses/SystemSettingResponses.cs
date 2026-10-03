namespace Kindi.API.Application.DTOs.responses;

/// <summary>Cấu hình chung của hệ thống dùng cho màn quản trị.</summary>
public class SystemSettingResponse
{
    public string SystemName { get; set; } = string.Empty;
    public string? SupportEmail { get; set; }
    public string? SupportPhone { get; set; }
    public string? Address { get; set; }
    public string? WorkingHours { get; set; }
    public string? FacebookUrl { get; set; }
    public string? YoutubeUrl { get; set; }
    public string? ZaloUrl { get; set; }
    public string? CopyrightText { get; set; }

    public string DefaultLanguage { get; set; } = "vi";
    public string TimeZone { get; set; } = "Asia/Ho_Chi_Minh";
    public string CurrencySymbol { get; set; } = "₫";
    public string DateFormat { get; set; } = "dd/MM/yyyy";

    public bool AllowRegistration { get; set; }
    public bool RequireEmailVerification { get; set; }
    public int MinPasswordLength { get; set; }
    public int AccessTokenMinutes { get; set; }
    public int RefreshTokenDays { get; set; }
    public int MaxFailedLoginAttempts { get; set; }
    public int LockoutMinutes { get; set; }
    public string ReferralCodePrefix { get; set; } = string.Empty;
    public int ReferralCodeLength { get; set; }
    public int CommissionAttributionDays { get; set; }

    public int MaxUploadSizeMb { get; set; }
    public string? AllowedImageExtensions { get; set; }
    public string? AllowedDocumentExtensions { get; set; }
    public int MaxImagesPerPost { get; set; }
    public bool RequirePostApproval { get; set; }
    public bool RequireGroupApproval { get; set; }
    public int AuditLogRetentionDays { get; set; }
    public bool EnableEmailNotification { get; set; }
    public string? NotificationSenderName { get; set; }
    public string? NotificationReplyTo { get; set; }

    public string? Note { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>Phần cấu hình công khai cho giao diện người dùng.</summary>
public class PublicSystemSettingResponse
{
    public string SystemName { get; set; } = string.Empty;
    public string? SupportEmail { get; set; }
    public string? SupportPhone { get; set; }
    public string? Address { get; set; }
    public string? WorkingHours { get; set; }
    public string? FacebookUrl { get; set; }
    public string? YoutubeUrl { get; set; }
    public string? ZaloUrl { get; set; }
    public string? CopyrightText { get; set; }
    public string DefaultLanguage { get; set; } = "vi";
    public string TimeZone { get; set; } = "Asia/Ho_Chi_Minh";
    public string CurrencySymbol { get; set; } = "₫";
    public string DateFormat { get; set; } = "dd/MM/yyyy";
    public bool AllowRegistration { get; set; }
}
