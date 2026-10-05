// SystemSetting.cs
namespace Kindi.API.Domain.Entities;

/// <summary>
/// Cấu hình chung của hệ thống: thông tin nền tảng hiển thị cho người dùng, ngôn ngữ và định dạng,
/// chính sách đăng ký/tài khoản, giới hạn tệp — nội dung và thiết lập thông báo.
/// </summary>
public class SystemSetting : BaseEntity
{
    /// <summary>Tên hệ thống dùng ở tiêu đề trang và nội dung thông báo.</summary>
    public string SystemName { get; set; } = "Kindi";

    /// <summary>Email tiếp nhận hỗ trợ.</summary>
    public string? SupportEmail { get; set; }

    /// <summary>Hotline hiển thị cho người dùng.</summary>
    public string? SupportPhone { get; set; }

    /// <summary>Địa chỉ trụ sở.</summary>
    public string? Address { get; set; }

    /// <summary>Giờ làm việc.</summary>
    public string? WorkingHours { get; set; }

    /// <summary>Liên kết Facebook.</summary>
    public string? FacebookUrl { get; set; }

    /// <summary>Liên kết Youtube.</summary>
    public string? YoutubeUrl { get; set; }

    /// <summary>Liên kết Zalo.</summary>
    public string? ZaloUrl { get; set; }
    /// <summary>Liên kết TikTok của nền tảng.</summary>
    public string? TiktokUrl { get; set; }
    /// <summary>Liên kết Instagram của nền tảng.</summary>
    public string? InstagramUrl { get; set; }
    /// <summary>Liên kết X của nền tảng.</summary>
    public string? XUrl { get; set; }
    /// <summary>Liên kết Threads của nền tảng.</summary>
    public string? ThreadsUrl { get; set; }
    /// <summary>Liên kết LinkedIn của nền tảng.</summary>
    public string? LinkedinUrl { get; set; }

    /// <summary>Dòng bản quyền ở chân trang.</summary>
    public string? CopyrightText { get; set; }
    /// <summary>Nội dung trang Chính sách bảo mật, soạn ở Cài đặt chung (định dạng HTML).</summary>
    public string? PrivacyPolicy { get; set; }
    /// <summary>Nội dung trang Điều khoản dịch vụ, soạn ở Cài đặt chung (định dạng HTML).</summary>
    public string? TermsOfService { get; set; }

    /// <summary>Ngôn ngữ mặc định của giao diện.</summary>
    public string DefaultLanguage { get; set; } = "vi";

    /// <summary>Múi giờ dùng để hiển thị thời gian.</summary>
    public string TimeZone { get; set; } = "Asia/Ho_Chi_Minh";

    /// <summary>Ký hiệu tiền tệ.</summary>
    public string CurrencySymbol { get; set; } = "₫";

    /// <summary>Định dạng ngày hiển thị.</summary>
    public string DateFormat { get; set; } = "dd/MM/yyyy";

    /// <summary>Cho phép người dùng tự đăng ký tài khoản.</summary>
    public bool AllowRegistration { get; set; } = true;

    /// <summary>Bắt buộc xác thực email sau khi đăng ký.</summary>
    public bool RequireEmailVerification { get; set; }

    /// <summary>Số ký tự tối thiểu của mật khẩu.</summary>
    public int MinPasswordLength { get; set; } = 8;

    /// <summary>Thời hạn của access token (phút).</summary>
    public int AccessTokenMinutes { get; set; } = 120;

    /// <summary>Thời hạn của refresh token (ngày).</summary>
    public int RefreshTokenDays { get; set; } = 30;

    /// <summary>Tỷ lệ thuế (%) áp dụng khi khai doanh thu giao dịch.</summary>
    public decimal RevenueTaxPercent { get; set; }

    /// <summary>Doanh thu nhập khi khai đã bao gồm thuế hay chưa.</summary>
    public bool RevenueTaxIncluded { get; set; }

    /// <summary>Số lần đăng nhập sai liên tiếp thì khoá tài khoản.</summary>
    public int MaxFailedLoginAttempts { get; set; } = 5;

    /// <summary>Thời gian khoá tài khoản (phút).</summary>
    public int LockoutMinutes { get; set; } = 15;

    /// <summary>Tiền tố của mã giới thiệu.</summary>
    public string ReferralCodePrefix { get; set; } = "KND";

    /// <summary>Số ký tự ngẫu nhiên của mã giới thiệu.</summary>
    public int ReferralCodeLength { get; set; } = 8;

    /// <summary>Số ngày ghi nhận hoa hồng tính từ lúc phát sinh sự kiện.</summary>
    public int CommissionAttributionDays { get; set; } = 30;

    /// <summary>Dung lượng tối đa của một tệp tải lên (MB).</summary>
    public int MaxUploadSizeMb { get; set; } = 10;

    /// <summary>Định dạng ảnh được phép tải lên, cách nhau dấu phẩy.</summary>
    public string? AllowedImageExtensions { get; set; } = "jpg,jpeg,png,webp";

    /// <summary>Định dạng tài liệu được phép tải lên, cách nhau dấu phẩy.</summary>
    public string? AllowedDocumentExtensions { get; set; } = "pdf,doc,docx,xls,xlsx";

    /// <summary>Số ảnh tối đa trong một bài viết.</summary>
    public int MaxImagesPerPost { get; set; } = 10;

    /// <summary>Bài viết phải duyệt trước khi hiển thị.</summary>
    public bool RequirePostApproval { get; set; }

    /// <summary>Nhóm ngành phải duyệt trước khi hiển thị.</summary>
    public bool RequireGroupApproval { get; set; }

    /// <summary>Số ngày lưu nhật ký hoạt động (0 = lưu không giới hạn).</summary>
    public int AuditLogRetentionDays { get; set; } = 365;

    /// <summary>Gửi email thông báo cho người dùng.</summary>
    public bool EnableEmailNotification { get; set; } = true;

    /// <summary>Tên người gửi trong email thông báo.</summary>
    public string? NotificationSenderName { get; set; }

    /// <summary>Địa chỉ nhận thư trả lời của email thông báo.</summary>
    public string? NotificationReplyTo { get; set; }

    /// <summary>Ghi chú nội bộ.</summary>
    public string? Note { get; set; }
}
