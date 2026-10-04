namespace Kindi.API.Domain.Enums;

/// <summary>
/// Mã màn hình — gom các hành động của cùng một màn hình/nghiệp vụ lại với nhau. Ma trận quyền hiển thị
/// theo cấu trúc Nhóm (module) → Màn hình → hành động (Xem/Sửa/Xoá), nên mỗi quyền phải khai màn hình.
/// </summary>
public static class PermissionScreens
{
    public const string Dashboard = "DASHBOARD";
    public const string CrmDashboard = "CRM_DASHBOARD";
    public const string Reports = "REPORTS";
    public const string AuditLogs = "AUDIT_LOGS";
    public const string SystemSettings = "SYSTEM_SETTINGS";

    public const string ReferralStats = "REFERRAL_STATS";
    public const string MyReferral = "MY_REFERRAL";
    public const string MyCommission = "MY_COMMISSION";

    public const string Users = "USERS";
    public const string Collaborators = "COLLABORATORS";
    public const string MyGroupBuying = "MY_GROUP_BUYING";
    public const string MyRequests = "MY_REQUESTS";
    public const string MyPosts = "MY_POSTS";
    public const string MyGroups = "MY_GROUPS";

    public const string Partners = "PARTNERS";
    public const string PartnerProducts = "PARTNER_PRODUCTS";
    public const string Companies = "COMPANIES";

    public const string PurchaseRequests = "PURCHASE_REQUESTS";
    public const string Offers = "OFFERS";
    public const string GroupBuying = "GROUP_BUYING";

    public const string Groups = "GROUPS";
    public const string SocialPosts = "SOCIAL_POSTS";

    public const string PermissionMatrix = "PERMISSION_MATRIX";
    public const string CommissionConfig = "COMMISSION_CONFIG";
    public const string Payouts = "PAYOUTS";
    public const string MembershipTiers = "MEMBERSHIP_TIERS";
    public const string BankAccounts = "BANK_ACCOUNTS";
    public const string Revenues = "REVENUES";
    public const string RevenueConfig = "REVENUE_CONFIG";
}

/// <summary>Một màn hình trong danh mục quyền: mã, tên hiển thị và thứ tự.</summary>
public sealed record PermissionScreenDefinition(
    string Code,
    string Name,
    string NameEn,
    int SortOrder);

/// <summary>
/// Danh mục màn hình — nguồn duy nhất để đặt tên nhóm hành động khi render ma trận quyền.
/// Thứ tự sắp xếp theo nhóm chức năng để các màn hình cùng khu vực đứng cạnh nhau.
/// </summary>
public static class PermissionScreenCatalog
{
    public static IReadOnlyList<PermissionScreenDefinition> All { get; } = new List<PermissionScreenDefinition>
    {
        new(PermissionScreens.Dashboard, "Bảng điều khiển", "Dashboard", 10),
        new(PermissionScreens.CrmDashboard, "Dashboard CRM", "CRM dashboard", 20),
        new(PermissionScreens.Reports, "Báo cáo", "Reports", 30),
        new(PermissionScreens.AuditLogs, "Nhật ký hệ thống", "Audit logs", 40),
        new(PermissionScreens.SystemSettings, "Cấu hình hệ thống", "System settings", 50),
        new(PermissionScreens.ReferralStats, "Thống kê giới thiệu", "Referral statistics", 60),

        new(PermissionScreens.Users, "Người dùng", "Users", 100),
        new(PermissionScreens.Collaborators, "Hồ sơ cộng tác viên", "Collaborators", 110),
        new(PermissionScreens.Partners, "Đối tác", "Partners", 120),
        new(PermissionScreens.PartnerProducts, "Sản phẩm đối tác", "Partner products", 130),
        new(PermissionScreens.Companies, "Công ty", "Companies", 140),
        new(PermissionScreens.PurchaseRequests, "Yêu cầu mua hàng", "Purchase requests", 150),
        new(PermissionScreens.Offers, "Offer", "Offers", 160),
        new(PermissionScreens.GroupBuying, "Yêu cầu mua chung", "Group buying", 170),
        new(PermissionScreens.Groups, "Nhóm", "Groups", 180),
        new(PermissionScreens.SocialPosts, "Bài đăng", "Social posts", 190),

        new(PermissionScreens.PermissionMatrix, "Phân quyền", "Permission matrix", 200),
        new(PermissionScreens.CommissionConfig, "Cấu hình hoa hồng", "Commission config", 210),
        new(PermissionScreens.Payouts, "Chi trả & giải ngân", "Payouts", 220),
        new(PermissionScreens.MembershipTiers, "Hạng thành viên", "Membership tiers", 230),
        new(PermissionScreens.BankAccounts, "Tài khoản ngân hàng", "Bank accounts", 240),
        new(PermissionScreens.Revenues, "Doanh thu giao dịch", "Transaction revenue", 250),
        new(PermissionScreens.RevenueConfig, "Cấu hình doanh thu", "Revenue config", 260),

        new(PermissionScreens.MyReferral, "Giới thiệu của tôi", "My referral", 300),
        new(PermissionScreens.MyCommission, "Hoa hồng của tôi", "My commission", 310),
        new(PermissionScreens.MyGroupBuying, "Mua chung của tôi", "My group buying", 320),
        new(PermissionScreens.MyRequests, "Yêu cầu của tôi", "My requests", 330),
        new(PermissionScreens.MyPosts, "Bài viết của tôi", "My posts", 340),
        new(PermissionScreens.MyGroups, "Nhóm của tôi", "My groups", 350)
    };

    private static readonly IReadOnlyDictionary<string, PermissionScreenDefinition> ByCode =
        All.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);

    /// <summary>Tên hiển thị của mặt màn hình; trả về rỗng nếu mã lạ.</summary>
    public static string ResolveName(string? code)
        => code is not null && ByCode.TryGetValue(code, out var definition) ? definition.Name : string.Empty;

    /// <summary>Thông tin màn hình theo mã; null nếu mã lạ.</summary>
    public static PermissionScreenDefinition? Resolve(string? code)
        => code is not null && ByCode.TryGetValue(code, out var definition) ? definition : null;
}
