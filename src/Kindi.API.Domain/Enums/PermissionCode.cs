namespace Kindi.API.Domain.Enums;

using Kindi.API.Domain.Attributes;

/// <summary>
/// Danh mục quyền của hệ thống. Nhóm hiển thị suy theo khu vực màn hình (xem PermissionCatalog),
/// chỉ khai ParentCode khi quyền thuộc nhóm dùng chung.
/// Mã quyền (P###) = "P" + giá trị số của member, ví dụ
/// <see cref="ViewUsers"/> = 20 → mã P020. Không đổi giá trị đã phát hành, chỉ thêm member mới.
/// </summary>
public enum PermissionCode
{
    [PermissionInfo("Xem dashboard", PermissionModule.System, PermissionKind.View, "/admin/dashboard", "GET /api/v1/dashboard")]
    ViewDashboard = 1,

    [PermissionInfo("Xem dashboard CRM", PermissionModule.System, PermissionKind.View, "/admin/admin-crm", "GET /api/v1/dashboard/crm")]
    ViewCrmDashboard = 2,

    [PermissionInfo("Xem báo cáo tổng quan", PermissionModule.System, PermissionKind.Action, null, "GET /api/v1/report/overview")]
    ViewReportOverview = 3,

    [PermissionInfo("Xem báo cáo thành viên", PermissionModule.System, PermissionKind.Action, null, "GET /api/v1/report/members")]
    ViewReportMembers = 4,

    [PermissionInfo("Xem báo cáo yêu cầu", PermissionModule.System, PermissionKind.Action, null, "GET /api/v1/report/requests")]
    ViewReportRequests = 5,

    [PermissionInfo("Xem báo cáo cộng đồng", PermissionModule.System, PermissionKind.Action, null, "GET /api/v1/report/social")]
    ViewReportSocial = 6,

    [PermissionInfo("Xem báo cáo xu hướng", PermissionModule.System, PermissionKind.Action, null, "GET /api/v1/report/trend")]
    ViewReportTrend = 7,

    [PermissionInfo("Xem lịch sử đăng nhập/đăng ký", PermissionModule.System, PermissionKind.Action, null, "GET /api/v1/admin/audit-logs/auth, GET /api/v1/admin/audit-logs/auth/{id}")]
    ViewAuthAuditLogs = 8,

    [PermissionInfo("Xem lịch sử thay đổi dữ liệu", PermissionModule.System, PermissionKind.Action, null, "GET /api/v1/admin/audit-logs/entity, GET /api/v1/admin/audit-logs/entity/{id}")]
    ViewEntityAuditLogs = 9,

    [PermissionInfo("Xem cấu hình hệ thống", PermissionModule.System, PermissionKind.View, "/admin/settings", null)]
    ViewSystemSettings = 10,

    [PermissionInfo("Xem thống kê giới thiệu", PermissionModule.Referral, PermissionKind.View, "/admin/referral-stats", "GET /api/v1/referrals/stats, GET /api/v1/referrals/stats/overview, GET /api/v1/referrals/stats/{referralCode}/events")]
    ViewReferralStats = 11,

    [PermissionInfo("Xem thống kê giới thiệu của tôi", PermissionModule.Referral, PermissionKind.View, "/user/my-referral",
        "GET /api/v1/referrals/me/stats, GET /api/v1/collaborators/me/referral-code")]
    ViewMyReferralStats = 12,

    [PermissionInfo("Xem danh sách người dùng", PermissionModule.User, PermissionKind.View, "/admin/users", "GET /api/v1/users")]
    ViewUsers = 20,

    [PermissionInfo("Đặt lại mật khẩu người dùng", PermissionModule.User, PermissionKind.Action, null, "POST /api/v1/users/{id}/reset-password")]
    ResetUserPassword = 21,

    [PermissionInfo("Gán / đổi role người dùng", PermissionModule.User, PermissionKind.Action, null, "PUT /api/v1/users/{id}/role")]
    AssignUserRole = 22,

    [PermissionInfo("Xem danh sách hồ sơ CTV", PermissionModule.User, PermissionKind.View, "/admin/collaborator", "GET /api/v1/collaborators, GET /api/v1/collaborators/{id}")]
    ViewCollaborators = 23,

    [PermissionInfo("Duyệt hồ sơ CTV", PermissionModule.User, PermissionKind.Action, null, "POST /api/v1/collaborators/{id}/approve")]
    ApproveCollaborator = 24,

    [PermissionInfo("Từ chối hồ sơ CTV", PermissionModule.User, PermissionKind.Action, null, "POST /api/v1/collaborators/{id}/reject")]
    RejectCollaborator = 25,

    [PermissionInfo("Xoá hồ sơ CTV", PermissionModule.User, PermissionKind.Action, null, "DELETE /api/v1/collaborators/{id}")]
    DeleteCollaborator = 26,

    [PermissionInfo("Xem hồ sơ CTV đã xoá / khôi phục", PermissionModule.User, PermissionKind.Action, null, "GET /api/v1/collaborators/deleted, POST /api/v1/collaborators/{id}/restore")]
    RestoreCollaborator = 27,

    [PermissionInfo("Xem đơn mua chung của tôi", PermissionModule.User, PermissionKind.View, "/user/my-group-buying",
        "GET /api/v1/groupbuyingrequests/public?mineOnly=true")]
    ViewMyGroupBuying = 28,

    [PermissionInfo("Xem yêu cầu của tôi", PermissionModule.User, PermissionKind.View, "/user/my-requests",
        "GET /api/v1/purchaserequests?mineOnly=true, GET /api/v1/offerrequests?mineOnly=true")]
    ViewMyRequests = 29,

    [PermissionInfo("Xem bài viết của tôi", PermissionModule.User, PermissionKind.View, "/user/my-posts",
        "GET /api/v1/social/posts?mineOnly=true")]
    ViewMyPosts = 30,

    [PermissionInfo("Xem nhóm của tôi", PermissionModule.User, PermissionKind.View, "/user/my-groups",
        "GET /api/v1/businessgroups/mine")]
    ViewMyGroups = 31,

    [PermissionInfo("Xem hoa hồng của tôi", PermissionModule.User, PermissionKind.View, "/user/my-commission",
        "GET /api/v1/commissions/me")]
    ViewMyCommission = 13,

    [PermissionInfo("Xem danh sách đối tác", PermissionModule.Partner, PermissionKind.View, "/admin/partner", "GET /api/v1/partners, GET /api/v1/partners/{id}, GET /api/v1/partners/deleted")]
    ViewPartners = 40,

    [PermissionInfo("Duyệt đối tác", PermissionModule.Partner, PermissionKind.Action, null, "POST /api/v1/partners/{id}/approve")]
    ApprovePartner = 41,

    [PermissionInfo("Từ chối đối tác", PermissionModule.Partner, PermissionKind.Action, null, "POST /api/v1/partners/{id}/reject")]
    RejectPartner = 42,

    [PermissionInfo("Kích hoạt đối tác", PermissionModule.Partner, PermissionKind.Action, null, "POST /api/v1/partners/{id}/activate")]
    ActivatePartner = 43,

    [PermissionInfo("Sửa hồ sơ đối tác", PermissionModule.Partner, PermissionKind.Action, null, "PUT /api/v1/partners/{id}")]
    UpdatePartner = 44,

    [PermissionInfo("Xoá / khôi phục đối tác", PermissionModule.Partner, PermissionKind.Action, null, "DELETE /api/v1/partners/{id}, POST /api/v1/partners/{id}/restore")]
    DeletePartner = 45,

    [PermissionInfo("Quản lý sản phẩm của đối tác", PermissionModule.Partner, PermissionKind.Action, null, "POST /api/v1/partners/{id}/products, PUT /api/v1/partners/{id}/products/{productId}, DELETE /api/v1/partners/{id}/products/{productId}")]
    ManagePartnerProducts = 46,

    [PermissionInfo("Xem công ty", PermissionModule.Partner, PermissionKind.Action, null, "GET /api/v1/companies, GET /api/v1/companies/{id}")]
    ViewCompanies = 47,

    [PermissionInfo("Tạo / sửa công ty", PermissionModule.Partner, PermissionKind.Action, null, "POST /api/v1/companies, PUT /api/v1/companies/{id}")]
    ManageCompanies = 48,

    [PermissionInfo("Xem yêu cầu mua hàng", PermissionModule.Purchase, PermissionKind.View, "/admin/purchase-requests", "GET /api/v1/purchaserequests/{id}")]
    ViewPurchaseRequests = 60,

    [PermissionInfo("Cập nhật trạng thái yêu cầu mua hàng", PermissionModule.Purchase, PermissionKind.Action, null, "PATCH /api/v1/purchaserequests/{id}/status")]
    UpdatePurchaseRequestStatus = 61,

    [PermissionInfo("Xuất dữ liệu yêu cầu mua hàng", PermissionModule.Purchase, PermissionKind.Action, null, "GET /api/v1/purchaserequests/export")]
    ExportPurchaseRequests = 62,

    [PermissionInfo("Xem offer", PermissionModule.Purchase, PermissionKind.View, "/admin/offers", "GET /api/v1/offerrequests/{id}")]
    ViewOfferRequests = 63,

    [PermissionInfo("Cập nhật trạng thái offer", PermissionModule.Purchase, PermissionKind.Action, null, "PATCH /api/v1/offerrequests/{id}/status")]
    UpdateOfferRequestStatus = 64,

    [PermissionInfo("Xoá / khôi phục offer", PermissionModule.Purchase, PermissionKind.Action, null, "DELETE /api/v1/offerrequests/{id}, POST /api/v1/offerrequests/{id}/restore")]
    DeleteOfferRequest = 65,

    [PermissionInfo("Xem yêu cầu mua chung", PermissionModule.Purchase, PermissionKind.View, "/admin/group-buying", "GET /api/v1/groupbuyingrequests, GET /api/v1/groupbuyingrequests/{id}")]
    ViewGroupBuyingRequests = 66,

    [PermissionInfo("Sửa / xoá yêu cầu mua chung", PermissionModule.Purchase, PermissionKind.Action, null, "PUT /api/v1/groupbuyingrequests/{id}, DELETE /api/v1/groupbuyingrequests/{id}, DELETE /api/v1/groupbuyingrequests/{id}/participants/{participantId}")]
    UpdateGroupBuyingRequest = 67,

    [PermissionInfo("Cập nhật trạng thái yêu cầu mua chung", PermissionModule.Purchase, PermissionKind.Action, null, "PUT /api/v1/groupbuyingrequests/{id}/status")]
    UpdateGroupBuyingRequestStatus = 68,

    [PermissionInfo("Xem nhóm", PermissionModule.Group, PermissionKind.View, "/admin/groups", "GET /api/v1/businessgroups, GET /api/v1/businessgroups/{id}")]
    ViewGroups = 70,

    [PermissionInfo("Tạo / sửa / xoá nhóm", PermissionModule.Group, PermissionKind.Action, null, "POST /api/v1/businessgroups, PUT /api/v1/businessgroups/{id}, DELETE /api/v1/businessgroups/{id}")]
    ManageGroups = 71,

    [PermissionInfo("Duyệt nhóm cộng đồng", PermissionModule.Group, PermissionKind.Action, null, "PUT /api/v1/businessgroups/community/{id}/approval")]
    ApproveCommunityGroup = 72,

    [PermissionInfo("Xem yêu cầu riêng của nhóm", PermissionModule.Group, PermissionKind.Action, null, "GET /api/v1/businessgroups/{id}/private-requests")]
    ViewGroupPrivateRequests = 73,

    [PermissionInfo("Sửa bài đăng trong nhóm", PermissionModule.Group, PermissionKind.Action, null, "PUT /api/v1/businessgroups/{id}/posts/{postId}")]
    UpdateGroupPost = 74,

    [PermissionInfo("Xem bài đăng (admin)", PermissionModule.Community, PermissionKind.View, "/admin/social-posts", "GET /api/v1/social/posts/admin, GET /api/v1/social/posts/pending")]
    ViewSocialPosts = 80,

    [PermissionInfo("Duyệt bài đăng", PermissionModule.Community, PermissionKind.Action, null, "POST /api/v1/social/posts/{id}/approve")]
    ApproveSocialPost = 81,

    [PermissionInfo("Từ chối bài đăng", PermissionModule.Community, PermissionKind.Action, null, "POST /api/v1/social/posts/{id}/reject")]
    RejectSocialPost = 82,

    [PermissionInfo("Ghim / bỏ ghim bài đăng", PermissionModule.Community, PermissionKind.Action, null, "POST /api/v1/social/posts/{id}/pin, POST /api/v1/social/posts/{id}/unpin")]
    PinSocialPost = 83,

    [PermissionInfo("Khôi phục bài đăng", PermissionModule.Community, PermissionKind.Action, null, "POST /api/v1/social/posts/{id}/restore")]
    RestoreSocialPost = 84,

    [PermissionInfo("Xem ma trận phân quyền", PermissionModule.SuperAdmin, PermissionKind.View, "/admin/permissions", "GET /api/v1/permissions")]
    ViewPermissions = 100,

    [PermissionInfo("Sửa quyền của role", PermissionModule.SuperAdmin, PermissionKind.Action, null, "PUT /api/v1/permissions/roles/{role}")]
    UpdateRolePermissions = 101,

    [PermissionInfo("Xem tài khoản SuperAdmin", PermissionModule.SuperAdmin, PermissionKind.Action, null, null)]
    ViewSuperAdminAccount = 102,

    [PermissionInfo("Xem toàn bộ audit log", PermissionModule.SuperAdmin, PermissionKind.View, "/admin/audit-logs", "GET /api/v1/admin/audit-logs/full")]
    ViewFullAuditLogs = 103,

    [PermissionInfo("Cấu hình quyền riêng cho người dùng", PermissionModule.SuperAdmin, PermissionKind.Action, null,
        "GET /api/v1/permissions/users, PUT /api/v1/permissions/users/{userId}, PUT /api/v1/permissions/users")]
    UpdateUserPermissions = 104,

    [PermissionInfo("Xem cấu hình hoa hồng", PermissionModule.SuperAdmin, PermissionKind.View, "/admin/settings",
        "GET /api/v1/commissions")]
    ViewCommissionConfigs = 105,

    [PermissionInfo("Sửa cấu hình hoa hồng", PermissionModule.SuperAdmin, PermissionKind.Action, null,
        "POST /api/v1/commissions, DELETE /api/v1/commissions/{id}")]
    UpdateCommissionConfigs = 106,
    [PermissionInfo("Cập nhật thông tin ngân hàng của tôi", PermissionModule.User, PermissionKind.Action, "/user/my-commission",
        "GET /api/v1/bank-accounts/me, PUT /api/v1/bank-accounts/me")]
    UpdateMyBankAccount = 14,

    [PermissionInfo("Gửi yêu cầu rút hoa hồng (kể cả rút sớm)", PermissionModule.User, PermissionKind.Action, "/user/my-commission",
        "POST /api/v1/payouts/withdrawals, POST /api/v1/payouts/{id}/cancel")]
    RequestCommissionWithdrawal = 15,

    [PermissionInfo("Xem chi trả hoa hồng và kỳ giải ngân", PermissionModule.SuperAdmin, PermissionKind.View, "/admin/finance",
        "GET /api/v1/payouts, GET /api/v1/payout-periods")]
    ViewPayouts = 107,

    [PermissionInfo("Xử lý chi trả hoa hồng", PermissionModule.SuperAdmin, PermissionKind.Action, null,
        "POST /api/v1/payouts/{id}/approve, POST /api/v1/payouts/{id}/reject, POST /api/v1/payouts/{id}/paid, POST /api/v1/payout-periods/{id}/close, POST /api/v1/payout-periods/{id}/pay")]
    ProcessPayouts = 108,

    [PermissionInfo("Cấu hình hạng thành viên và phí rút sớm", PermissionModule.SuperAdmin, PermissionKind.Action, "/admin/finance",
        "POST/PUT/DELETE /api/v1/membership-tiers, PUT /api/v1/payout-settings")]
    ManageMembershipTiers = 109,

    [PermissionInfo("Xác minh thông tin ngân hàng", PermissionModule.SuperAdmin, PermissionKind.Action, null,
        "PUT /api/v1/bank-accounts/{userId}/verification, GET /api/v1/bank-accounts")]
    VerifyBankAccounts = 110,
}

/// <summary>Tiện ích chuyển giữa member enum và mã P### dùng trong DB/claim/UI.</summary>
public static class PermissionCodeExtensions
{
    /// <summary>Mã quyền dạng P### của một member.</summary>
    public static string ToCode(this PermissionCode permission) => $"P{(int)permission:D3}";

    /// <summary>Đọc mã P### (không phân biệt hoa/thường) thành member; trả null nếu mã lạ.</summary>
    public static PermissionCode? FromCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;

        var raw = code.Trim();
        if (raw.StartsWith("P", StringComparison.OrdinalIgnoreCase)) raw = raw[1..];
        return int.TryParse(raw, out var number) && Enum.IsDefined(typeof(PermissionCode), number)
            ? (PermissionCode)number
            : null;
    }

}