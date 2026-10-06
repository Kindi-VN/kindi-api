namespace Kindi.API.Application.DTOs.responses;

/// <summary>
/// Chi tiết một tài khoản cho màn quản lý người dùng: thông tin tài khoản + hồ sơ CTV và hồ sơ
/// đối tác đang liên kết với tài khoản đó (một điểm xem/sửa tập trung).
/// </summary>
public class UserDetailResponse
{
    /// <summary>Thông tin tài khoản (bảng <c>Users</c>) — nguồn duy nhất của thông tin cá nhân.</summary>
    public UserInfoResponse User { get; set; } = new();

    /// <summary>Hồ sơ cộng tác viên liên kết; null nếu tài khoản chưa có hồ sơ CTV.</summary>
    public UserCollaboratorSummaryDto? Collaborator { get; set; }

    /// <summary>Hồ sơ đối tác liên kết; null nếu tài khoản chưa đăng ký đối tác.</summary>
    public UserPartnerSummaryDto? Partner { get; set; }
}

/// <summary>Hồ sơ cộng tác viên liên kết với tài khoản (rút gọn cho màn người dùng).</summary>
public class UserCollaboratorSummaryDto
{
    public Guid Id { get; set; }
    public string? CollaboratorCode { get; set; }
    public string? ReferralCode { get; set; }
    public string? Position { get; set; }
    public string? BusinessName { get; set; }
    public string? BusinessFieldName { get; set; }
    public string? Website { get; set; }
    public string? Address { get; set; }
    public bool IsApproved { get; set; }
    public int Level { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
}

/// <summary>Hồ sơ đối tác liên kết với tài khoản (rút gọn cho màn người dùng).</summary>
public class UserPartnerSummaryDto
{
    public Guid Id { get; set; }
    public string PartnerCode { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? CompanyTax { get; set; }
    public string? CompanyAddress { get; set; }
    public string? CompanyWebsite { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ApprovedAt { get; set; }
}
