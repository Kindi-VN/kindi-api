namespace Kindi.API.Domain.Enums;

/// <summary>
/// Mã màn hình — gom các hành động của cùng một màn hình/nghiệp vụ lại với nhau. Cây quyền hiển thị
/// theo cấu trúc Nhóm (group) → Màn hình (screen) → hành động (action), nên mỗi quyền phải khai màn hình.
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

    /// <summary>Danh mục lĩnh vực hoạt động (dùng chung cho công ty và hồ sơ CTV/đối tác).</summary>
    public const string BusinessFields = "BUSINESS_FIELDS";

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

/// <summary>
/// Một màn hình trong danh mục quyền: mã, tên hiển thị, nhóm chứa nó (nhóm quyền) và thứ tự.
/// <see cref="Group"/> chính là <c>ParentCode</c> của node màn hình trong cây quyền.
/// </summary>
public sealed record PermissionScreenDefinition(
    string Code,
    string Name,
    string NameEn,
    string Group,
    int SortOrder);

/// <summary>
/// Danh mục màn hình — nguồn duy nhất để đặt tên nhóm hành động khi render cây quyền.
/// Thứ tự sắp xếp theo nhóm chức năng để các màn hình cùng khu vực đứng cạnh nhau.
/// </summary>
public static class PermissionScreenCatalog
{
    public static IReadOnlyList<PermissionScreenDefinition> All { get; } = new List<PermissionScreenDefinition>
    {
        new(PermissionScreens.Dashboard, "Bảng điều khiển", "Dashboard", PermissionGroupCodes.Admin, 10),
        new(PermissionScreens.CrmDashboard, "Dashboard CRM", "CRM dashboard", PermissionGroupCodes.Admin, 20),
        new(PermissionScreens.Reports, "Báo cáo", "Reports", PermissionGroupCodes.Admin, 30),
        new(PermissionScreens.AuditLogs, "Nhật ký hệ thống", "Audit logs", PermissionGroupCodes.Admin, 40),
        new(PermissionScreens.SystemSettings, "Cấu hình hệ thống", "System settings", PermissionGroupCodes.Admin, 50),
        new(PermissionScreens.ReferralStats, "Thống kê giới thiệu", "Referral statistics", PermissionGroupCodes.Admin, 60),

        new(PermissionScreens.Users, "Người dùng", "Users", PermissionGroupCodes.Admin, 100),
        new(PermissionScreens.Collaborators, "Hồ sơ cộng tác viên", "Collaborators", PermissionGroupCodes.Admin, 110),
        new(PermissionScreens.Partners, "Đối tác", "Partners", PermissionGroupCodes.Admin, 120),
        new(PermissionScreens.PartnerProducts, "Sản phẩm đối tác", "Partner products", PermissionGroupCodes.Admin, 130),
        new(PermissionScreens.Companies, "Công ty", "Companies", PermissionGroupCodes.Admin, 140),
        new(PermissionScreens.PurchaseRequests, "Yêu cầu mua hàng", "Purchase requests", PermissionGroupCodes.Admin, 150),
        new(PermissionScreens.Offers, "Offer", "Offers", PermissionGroupCodes.Admin, 160),
        new(PermissionScreens.GroupBuying, "Yêu cầu mua chung", "Group buying", PermissionGroupCodes.Admin, 170),
        new(PermissionScreens.Groups, "Nhóm", "Groups", PermissionGroupCodes.Admin, 180),
        new(PermissionScreens.SocialPosts, "Bài đăng", "Social posts", PermissionGroupCodes.Admin, 190),

        new(PermissionScreens.PermissionMatrix, "Phân quyền", "Permission matrix", PermissionGroupCodes.Admin, 200),
        new(PermissionScreens.CommissionConfig, "Cấu hình hoa hồng", "Commission config", PermissionGroupCodes.Admin, 210),
        new(PermissionScreens.Payouts, "Chi trả & giải ngân", "Payouts", PermissionGroupCodes.Admin, 220),
        new(PermissionScreens.MembershipTiers, "Hạng thành viên", "Membership tiers", PermissionGroupCodes.Admin, 230),
        new(PermissionScreens.BankAccounts, "Tài khoản ngân hàng", "Bank accounts", PermissionGroupCodes.Admin, 240),
        new(PermissionScreens.Revenues, "Doanh thu giao dịch", "Transaction revenue", PermissionGroupCodes.Admin, 250),
        new(PermissionScreens.RevenueConfig, "Cấu hình doanh thu", "Revenue config", PermissionGroupCodes.Admin, 260),

        new(PermissionScreens.MyReferral, "Giới thiệu của tôi", "My referral", PermissionGroupCodes.Member, 300),
        new(PermissionScreens.MyCommission, "Hoa hồng của tôi", "My commission", PermissionGroupCodes.Member, 310),
        new(PermissionScreens.MyGroupBuying, "Mua chung của tôi", "My group buying", PermissionGroupCodes.Member, 320),
        new(PermissionScreens.MyRequests, "Yêu cầu của tôi", "My requests", PermissionGroupCodes.Member, 330),
        new(PermissionScreens.MyPosts, "Bài viết của tôi", "My posts", PermissionGroupCodes.Member, 340),
        new(PermissionScreens.MyGroups, "Nhóm của tôi", "My groups", PermissionGroupCodes.Member, 350)
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
