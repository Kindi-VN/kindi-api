namespace Kindi.API.Domain.Enums;

using Kindi.API.Domain.Attributes;

/// <summary>
/// Danh mục quyền của hệ thống. Nhóm hiển thị suy theo khu vực màn hình (xem PermissionCatalog),
/// chỉ khai ParentCode khi quyền thuộc nhóm dùng chung.
/// Mã quyền (P###) = "P" + giá trị số của member, ví dụ
/// <see cref="ViewUsers"/> = 20 → mã P020. Không đổi giá trị đã phát hành, chỉ thêm member mới.
/// Mỗi quyền khai thêm màn hình (Screen) để ma trận hiển thị theo Nhóm → Màn hình → hành động.
/// </summary>
public enum PermissionCode
{
    [PermissionInfo("Xem dashboard", PermissionModule.System, PermissionKind.View, "/admin/dashboard", "GET /api/v1/dashboard", Screen = PermissionScreens.Dashboard)]
    ViewDashboard = 1,

    [PermissionInfo("Xem dashboard CRM", PermissionModule.System, PermissionKind.View, "/admin/admin-crm", "GET /api/v1/dashboard/crm", Screen = PermissionScreens.CrmDashboard)]
    ViewCrmDashboard = 2,

    [PermissionInfo("Xem báo cáo tổng quan", PermissionModule.System, PermissionKind.View, null, "GET /api/v1/report/overview", Screen = PermissionScreens.Reports)]
    ViewReportOverview = 3,

    [PermissionInfo("Xem báo cáo thành viên", PermissionModule.System, PermissionKind.View, null, "GET /api/v1/report/members", Screen = PermissionScreens.Reports)]
    ViewReportMembers = 4,

    [PermissionInfo("Xem báo cáo yêu cầu", PermissionModule.System, PermissionKind.View, null, "GET /api/v1/report/requests", Screen = PermissionScreens.Reports)]
    ViewReportRequests = 5,

    [PermissionInfo("Xem báo cáo cộng đồng", PermissionModule.System, PermissionKind.View, null, "GET /api/v1/report/social", Screen = PermissionScreens.Reports)]
    ViewReportSocial = 6,

    [PermissionInfo("Xem báo cáo xu hướng", PermissionModule.System, PermissionKind.View, null, "GET /api/v1/report/trend", Screen = PermissionScreens.Reports)]
    ViewReportTrend = 7,

    [PermissionInfo("Xem lịch sử đăng nhập/đăng ký", PermissionModule.System, PermissionKind.View, null, "GET /api/v1/admin/audit-logs/auth, GET /api/v1/admin/audit-logs/auth/{id}", Screen = PermissionScreens.AuditLogs)]
    ViewAuthAuditLogs = 8,

    [PermissionInfo("Xem lịch sử thay đổi dữ liệu", PermissionModule.System, PermissionKind.View, null, "GET /api/v1/admin/audit-logs/entity, GET /api/v1/admin/audit-logs/entity/{id}", Screen = PermissionScreens.AuditLogs)]
    ViewEntityAuditLogs = 9,

    [PermissionInfo("Xem cấu hình hệ thống", PermissionModule.System, PermissionKind.View, "/admin/settings", null, Screen = PermissionScreens.SystemSettings)]
    ViewSystemSettings = 10,

    [PermissionInfo("Xem thống kê giới thiệu", PermissionModule.Referral, PermissionKind.View, "/admin/referral-stats", "GET /api/v1/referrals/stats, GET /api/v1/referrals/stats/overview, GET /api/v1/referrals/stats/{referralCode}/events", Screen = PermissionScreens.ReferralStats)]
    ViewReferralStats = 11,

    [PermissionInfo("Xem thống kê giới thiệu của tôi", PermissionModule.Referral, PermissionKind.View, "/user/my-referral",
        "GET /api/v1/referrals/me/stats, GET /api/v1/collaborators/me/referral-code", Screen = PermissionScreens.MyReferral)]
    ViewMyReferralStats = 12,

    [PermissionInfo("Xem danh sách người dùng", PermissionModule.User, PermissionKind.View, "/admin/users", "GET /api/v1/users", Screen = PermissionScreens.Users)]
    ViewUsers = 20,

    [PermissionInfo("Đặt lại mật khẩu người dùng", PermissionModule.User, PermissionKind.Action, null, "POST /api/v1/users/{id}/reset-password", Screen = PermissionScreens.Users)]
    ResetUserPassword = 21,

    [PermissionInfo("Gán / đổi role người dùng", PermissionModule.User, PermissionKind.Action, null, "PUT /api/v1/users/{id}/role", Screen = PermissionScreens.Users)]
    AssignUserRole = 22,

    [PermissionInfo("Xem danh sách hồ sơ CTV", PermissionModule.User, PermissionKind.View, "/admin/collaborator", "GET /api/v1/collaborators, GET /api/v1/collaborators/{id}", Screen = PermissionScreens.Collaborators)]
    ViewCollaborators = 23,

    [PermissionInfo("Duyệt hồ sơ CTV", PermissionModule.User, PermissionKind.Action, null, "POST /api/v1/collaborators/{id}/approve", Screen = PermissionScreens.Collaborators)]
    ApproveCollaborator = 24,

    [PermissionInfo("Từ chối hồ sơ CTV", PermissionModule.User, PermissionKind.Action, null, "POST /api/v1/collaborators/{id}/reject", Screen = PermissionScreens.Collaborators)]
    RejectCollaborator = 25,

    [PermissionInfo("Xoá hồ sơ CTV", PermissionModule.User, PermissionKind.Delete, null, "DELETE /api/v1/collaborators/{id}", Screen = PermissionScreens.Collaborators)]
    DeleteCollaborator = 26,

    // Mã gộp cũ (xem hồ sơ đã xoá + khôi phục). Giữ trong giai đoạn chuyển tiếp để token cũ không bị chặn;
    // chức năng đã chuyển sang P115, sẽ gỡ sau khi token cũ hết hạn.
    [PermissionInfo("Xem hồ sơ CTV đã xoá / khôi phục", PermissionModule.User, PermissionKind.Action, null, "GET /api/v1/collaborators/deleted, POST /api/v1/collaborators/{id}/restore", Screen = PermissionScreens.Collaborators)]
    RestoreCollaborator = 27,

    [PermissionInfo("Xem đơn mua chung của tôi", PermissionModule.User, PermissionKind.View, "/user/my-group-buying",
        "GET /api/v1/groupbuyingrequests/public?mineOnly=true", Screen = PermissionScreens.MyGroupBuying)]
    ViewMyGroupBuying = 28,

    [PermissionInfo("Xem yêu cầu của tôi", PermissionModule.User, PermissionKind.View, "/user/my-requests",
        "GET /api/v1/purchaserequests?mineOnly=true, GET /api/v1/offerrequests?mineOnly=true", Screen = PermissionScreens.MyRequests)]
    ViewMyRequests = 29,

    [PermissionInfo("Xem bài viết của tôi", PermissionModule.User, PermissionKind.View, "/user/my-posts",
        "GET /api/v1/social/posts?mineOnly=true", Screen = PermissionScreens.MyPosts)]
    ViewMyPosts = 30,

    [PermissionInfo("Xem nhóm của tôi", PermissionModule.User, PermissionKind.View, "/user/my-groups",
        "GET /api/v1/businessgroups/mine", Screen = PermissionScreens.MyGroups)]
    ViewMyGroups = 31,

    [PermissionInfo("Xem hoa hồng của tôi", PermissionModule.User, PermissionKind.View, "/user/my-commission",
        "GET /api/v1/commissions/me", Screen = PermissionScreens.MyCommission)]
    ViewMyCommission = 13,

    [PermissionInfo("Xem danh sách đối tác", PermissionModule.Partner, PermissionKind.View, "/admin/partner", "GET /api/v1/partners, GET /api/v1/partners/{id}, GET /api/v1/partners/deleted", Screen = PermissionScreens.Partners)]
    ViewPartners = 40,

    [PermissionInfo("Duyệt đối tác", PermissionModule.Partner, PermissionKind.Action, null, "POST /api/v1/partners/{id}/approve", Screen = PermissionScreens.Partners)]
    ApprovePartner = 41,

    [PermissionInfo("Từ chối đối tác", PermissionModule.Partner, PermissionKind.Action, null, "POST /api/v1/partners/{id}/reject", Screen = PermissionScreens.Partners)]
    RejectPartner = 42,

    [PermissionInfo("Kích hoạt đối tác", PermissionModule.Partner, PermissionKind.Action, null, "POST /api/v1/partners/{id}/activate", Screen = PermissionScreens.Partners)]
    ActivatePartner = 43,

    [PermissionInfo("Sửa hồ sơ đối tác", PermissionModule.Partner, PermissionKind.Update, null, "PUT /api/v1/partners/{id}", Screen = PermissionScreens.Partners)]
    UpdatePartner = 44,

    [PermissionInfo("Xoá đối tác", PermissionModule.Partner, PermissionKind.Delete, null, "DELETE /api/v1/partners/{id}", Screen = PermissionScreens.Partners)]
    DeletePartner = 45,

    [PermissionInfo("Thêm / sửa sản phẩm của đối tác", PermissionModule.Partner, PermissionKind.Update, null, "POST /api/v1/partners/{id}/products, PUT /api/v1/partners/{id}/products/{productId}", Screen = PermissionScreens.PartnerProducts)]
    ManagePartnerProducts = 46,

    [PermissionInfo("Xem công ty", PermissionModule.Partner, PermissionKind.View, null, "GET /api/v1/companies, GET /api/v1/companies/{id}", Screen = PermissionScreens.Companies)]
    ViewCompanies = 47,

    [PermissionInfo("Tạo / sửa công ty", PermissionModule.Partner, PermissionKind.Update, null, "POST /api/v1/companies, PUT /api/v1/companies/{id}", Screen = PermissionScreens.Companies)]
    ManageCompanies = 48,

    [PermissionInfo("Xem yêu cầu mua hàng", PermissionModule.Purchase, PermissionKind.View, "/admin/purchase-requests", "GET /api/v1/purchaserequests/{id}", Screen = PermissionScreens.PurchaseRequests)]
    ViewPurchaseRequests = 60,

    [PermissionInfo("Cập nhật trạng thái yêu cầu mua hàng", PermissionModule.Purchase, PermissionKind.Action, null, "PATCH /api/v1/purchaserequests/{id}/status", Screen = PermissionScreens.PurchaseRequests)]
    UpdatePurchaseRequestStatus = 61,

    [PermissionInfo("Xuất dữ liệu yêu cầu mua hàng", PermissionModule.Purchase, PermissionKind.Action, null, "GET /api/v1/purchaserequests/export", Screen = PermissionScreens.PurchaseRequests)]
    ExportPurchaseRequests = 62,

    [PermissionInfo("Xem offer", PermissionModule.Purchase, PermissionKind.View, "/admin/offers", "GET /api/v1/offerrequests/{id}", Screen = PermissionScreens.Offers)]
    ViewOfferRequests = 63,

    [PermissionInfo("Cập nhật trạng thái offer", PermissionModule.Purchase, PermissionKind.Action, null, "PATCH /api/v1/offerrequests/{id}/status", Screen = PermissionScreens.Offers)]
    UpdateOfferRequestStatus = 64,

    [PermissionInfo("Xoá offer", PermissionModule.Purchase, PermissionKind.Delete, null, "DELETE /api/v1/offerrequests/{id}", Screen = PermissionScreens.Offers)]
    DeleteOfferRequest = 65,

    [PermissionInfo("Xem yêu cầu mua chung", PermissionModule.Purchase, PermissionKind.View, "/admin/group-buying", "GET /api/v1/groupbuyingrequests, GET /api/v1/groupbuyingrequests/{id}", Screen = PermissionScreens.GroupBuying)]
    ViewGroupBuyingRequests = 66,

    [PermissionInfo("Sửa yêu cầu mua chung", PermissionModule.Purchase, PermissionKind.Update, null, "PUT /api/v1/groupbuyingrequests/{id}", Screen = PermissionScreens.GroupBuying)]
    UpdateGroupBuyingRequest = 67,

    [PermissionInfo("Cập nhật trạng thái yêu cầu mua chung", PermissionModule.Purchase, PermissionKind.Action, null, "PUT /api/v1/groupbuyingrequests/{id}/status", Screen = PermissionScreens.GroupBuying)]
    UpdateGroupBuyingRequestStatus = 68,

    [PermissionInfo("Xem nhóm", PermissionModule.Group, PermissionKind.View, "/admin/groups", "GET /api/v1/businessgroups, GET /api/v1/businessgroups/{id}", Screen = PermissionScreens.Groups)]
    ViewGroups = 70,

    [PermissionInfo("Tạo / sửa nhóm", PermissionModule.Group, PermissionKind.Update, null, "POST /api/v1/businessgroups, PUT /api/v1/businessgroups/{id}", Screen = PermissionScreens.Groups)]
    ManageGroups = 71,

    [PermissionInfo("Duyệt nhóm cộng đồng", PermissionModule.Group, PermissionKind.Action, null, "PUT /api/v1/businessgroups/community/{id}/approval", Screen = PermissionScreens.Groups)]
    ApproveCommunityGroup = 72,

    [PermissionInfo("Xem yêu cầu riêng của nhóm", PermissionModule.Group, PermissionKind.View, null, "GET /api/v1/businessgroups/{id}/private-requests", Screen = PermissionScreens.Groups)]
    ViewGroupPrivateRequests = 73,

    [PermissionInfo("Sửa bài đăng trong nhóm", PermissionModule.Group, PermissionKind.Update, null, "PUT /api/v1/businessgroups/{id}/posts/{postId}", Screen = PermissionScreens.Groups)]
    UpdateGroupPost = 74,

    [PermissionInfo("Xem bài đăng (admin)", PermissionModule.Community, PermissionKind.View, "/admin/social-posts", "GET /api/v1/social/posts/admin, GET /api/v1/social/posts/pending", Screen = PermissionScreens.SocialPosts)]
    ViewSocialPosts = 80,

    [PermissionInfo("Duyệt bài đăng", PermissionModule.Community, PermissionKind.Action, null, "POST /api/v1/social/posts/{id}/approve", Screen = PermissionScreens.SocialPosts)]
    ApproveSocialPost = 81,

    [PermissionInfo("Từ chối bài đăng", PermissionModule.Community, PermissionKind.Action, null, "POST /api/v1/social/posts/{id}/reject", Screen = PermissionScreens.SocialPosts)]
    RejectSocialPost = 82,

    [PermissionInfo("Ghim / bỏ ghim bài đăng", PermissionModule.Community, PermissionKind.Action, null, "POST /api/v1/social/posts/{id}/pin, POST /api/v1/social/posts/{id}/unpin", Screen = PermissionScreens.SocialPosts)]
    PinSocialPost = 83,

    [PermissionInfo("Khôi phục bài đăng", PermissionModule.Community, PermissionKind.Action, null, "POST /api/v1/social/posts/{id}/restore", Screen = PermissionScreens.SocialPosts)]
    RestoreSocialPost = 84,

    [PermissionInfo("Xem ma trận phân quyền", PermissionModule.SuperAdmin, PermissionKind.View, "/admin/permissions", "GET /api/v1/permissions", Screen = PermissionScreens.PermissionMatrix)]
    ViewPermissions = 100,

    [PermissionInfo("Sửa quyền của role", PermissionModule.SuperAdmin, PermissionKind.Update, null, "PUT /api/v1/permissions/roles/{role}", Screen = PermissionScreens.PermissionMatrix)]
    UpdateRolePermissions = 101,

    [PermissionInfo("Xem tài khoản SuperAdmin", PermissionModule.SuperAdmin, PermissionKind.Action, null, null, Screen = PermissionScreens.PermissionMatrix)]
    ViewSuperAdminAccount = 102,

    [PermissionInfo("Xem toàn bộ audit log", PermissionModule.SuperAdmin, PermissionKind.View, "/admin/audit-logs", "GET /api/v1/admin/audit-logs/full", Screen = PermissionScreens.AuditLogs)]
    ViewFullAuditLogs = 103,

    [PermissionInfo("Cấu hình quyền riêng cho người dùng", PermissionModule.SuperAdmin, PermissionKind.Action, null,
        "GET /api/v1/permissions/users, PUT /api/v1/permissions/users/{userId}, PUT /api/v1/permissions/users", Screen = PermissionScreens.PermissionMatrix)]
    UpdateUserPermissions = 104,

    [PermissionInfo("Xem cấu hình hoa hồng", PermissionModule.SuperAdmin, PermissionKind.View, "/admin/settings",
        "GET /api/v1/commissions", Screen = PermissionScreens.CommissionConfig)]
    ViewCommissionConfigs = 105,

    [PermissionInfo("Thêm / sửa cấu hình hoa hồng", PermissionModule.SuperAdmin, PermissionKind.Update, null,
        "POST /api/v1/commissions", Screen = PermissionScreens.CommissionConfig)]
    UpdateCommissionConfigs = 106,
    [PermissionInfo("Cập nhật thông tin ngân hàng của tôi", PermissionModule.User, PermissionKind.Update, "/user/my-commission",
        "GET /api/v1/bank-accounts/me, PUT /api/v1/bank-accounts/me", Screen = PermissionScreens.MyCommission)]
    UpdateMyBankAccount = 14,

    [PermissionInfo("Gửi yêu cầu rút hoa hồng (kể cả rút sớm)", PermissionModule.User, PermissionKind.Action, "/user/my-commission",
        "POST /api/v1/payouts/withdrawals, POST /api/v1/payouts/{id}/cancel", Screen = PermissionScreens.MyCommission)]
    RequestCommissionWithdrawal = 15,

    [PermissionInfo("Xem chi trả hoa hồng và kỳ giải ngân", PermissionModule.SuperAdmin, PermissionKind.View, "/admin/finance",
        "GET /api/v1/payouts, GET /api/v1/payout-periods", Screen = PermissionScreens.Payouts)]
    ViewPayouts = 107,

    [PermissionInfo("Xử lý chi trả hoa hồng", PermissionModule.SuperAdmin, PermissionKind.Action, null,
        "POST /api/v1/payouts/{id}/approve, POST /api/v1/payouts/{id}/reject, POST /api/v1/payouts/{id}/paid, POST /api/v1/payout-periods/{id}/close, POST /api/v1/payout-periods/{id}/pay", Screen = PermissionScreens.Payouts)]
    ProcessPayouts = 108,

    // Mã gộp cũ (thêm/sửa/xoá hạng thành viên). Giữ trong giai đoạn chuyển tiếp để token cũ không bị chặn;
    // chức năng đã tách sang P125 (xem) / P126 (sửa) / P127 (xoá), sẽ gỡ sau khi token cũ hết hạn.
    [PermissionInfo("Cấu hình hạng thành viên và phí rút sớm", PermissionModule.SuperAdmin, PermissionKind.Action, "/admin/finance",
        "POST/PUT/DELETE /api/v1/membership-tiers, PUT /api/v1/payout-settings", Screen = PermissionScreens.MembershipTiers)]
    ManageMembershipTiers = 109,

    [PermissionInfo("Xác minh thông tin ngân hàng", PermissionModule.SuperAdmin, PermissionKind.Update, null,
        "PUT /api/v1/bank-accounts/{userId}/verification", Screen = PermissionScreens.BankAccounts)]
    VerifyBankAccounts = 110,

    [PermissionInfo("Sửa cài đặt chung", PermissionModule.System, PermissionKind.Update, null, "PUT /api/v1/settings", Screen = PermissionScreens.SystemSettings)]
    UpdateSystemSettings = 111,

    [PermissionInfo("Khai và chốt doanh thu giao dịch", PermissionModule.Revenue, PermissionKind.Action, null,
        "POST /api/v1/revenues, PUT /api/v1/revenues/{id}, POST /api/v1/revenues/{id}/confirm", Screen = PermissionScreens.Revenues)]
    ManageTransactionRevenue = 112,

    [PermissionInfo("Xem thống kê doanh thu giao dịch", PermissionModule.Revenue, PermissionKind.View, "/admin/revenue",
        "GET /api/v1/revenues, GET /api/v1/revenues/stats", Screen = PermissionScreens.Revenues)]
    ViewTransactionRevenue = 113,

    // Mã gộp cũ (xem/thêm/sửa/xoá cấu hình doanh thu). Giữ trong giai đoạn chuyển tiếp để token cũ không bị chặn;
    // chức năng đã tách sang P129 (xem) / P130 (sửa) / P131 (xoá), sẽ gỡ sau khi token cũ hết hạn.
    [PermissionInfo("Quản lý cấu hình doanh thu", PermissionModule.Revenue, PermissionKind.Action, "/admin/revenue",
        "GET/POST/PUT/DELETE /api/v1/RevenueExpenseTypes, PUT /api/v1/RevenueExpenseTypes/assign, GET/PUT /api/v1/RevenueExpenseTypes/config", Screen = PermissionScreens.RevenueConfig)]
    ManageRevenueConfig = 114,

    // ===== Quyền tách Xem / Sửa / Xoá (cấp từ P115 trở đi) =====

    [PermissionInfo("Xem & khôi phục hồ sơ CTV đã xoá", PermissionModule.User, PermissionKind.View, "/admin/collaborator",
        "GET /api/v1/collaborators/deleted, POST /api/v1/collaborators/{id}/restore", Screen = PermissionScreens.Collaborators,
        Replaces = new[] { "P027" })]
    ViewRestoreCollaborator = 115,

    [PermissionInfo("Khôi phục đối tác", PermissionModule.Partner, PermissionKind.Delete, null,
        "POST /api/v1/partners/{id}/restore", Screen = PermissionScreens.Partners,
        Replaces = new[] { "P045" })]
    RestorePartner = 116,

    [PermissionInfo("Xoá sản phẩm của đối tác", PermissionModule.Partner, PermissionKind.Delete, null,
        "DELETE /api/v1/partners/{id}/products/{productId}", Screen = PermissionScreens.PartnerProducts,
        Replaces = new[] { "P046" })]
    DeletePartnerProduct = 118,

    [PermissionInfo("Khôi phục offer", PermissionModule.Purchase, PermissionKind.Delete, null,
        "POST /api/v1/offerrequests/{id}/restore", Screen = PermissionScreens.Offers,
        Replaces = new[] { "P065" })]
    RestoreOfferRequest = 119,

    [PermissionInfo("Xoá yêu cầu mua chung", PermissionModule.Purchase, PermissionKind.Delete, null,
        "DELETE /api/v1/groupbuyingrequests/{id}, DELETE /api/v1/groupbuyingrequests/{id}/participants/{participantId}", Screen = PermissionScreens.GroupBuying,
        Replaces = new[] { "P067" })]
    DeleteGroupBuyingRequest = 120,

    [PermissionInfo("Xoá nhóm", PermissionModule.Group, PermissionKind.Delete, null,
        "DELETE /api/v1/businessgroups/{id}", Screen = PermissionScreens.Groups,
        Replaces = new[] { "P071" })]
    DeleteGroup = 121,

    [PermissionInfo("Xoá cấu hình hoa hồng", PermissionModule.SuperAdmin, PermissionKind.Delete, null,
        "DELETE /api/v1/commissions/{id}", Screen = PermissionScreens.CommissionConfig,
        Replaces = new[] { "P106" })]
    DeleteCommissionConfig = 124,

    [PermissionInfo("Xem hạng thành viên", PermissionModule.SuperAdmin, PermissionKind.View, "/admin/finance",
        "GET /api/v1/membership-tiers", Screen = PermissionScreens.MembershipTiers,
        Replaces = new[] { "P109", "P107" })]
    ViewMembershipTiers = 125,

    [PermissionInfo("Sửa hạng thành viên", PermissionModule.SuperAdmin, PermissionKind.Update, null,
        "POST /api/v1/membership-tiers, PUT /api/v1/membership-tiers/{id}, POST /api/v1/membership-tiers/evaluate", Screen = PermissionScreens.MembershipTiers,
        Replaces = new[] { "P109" })]
    UpdateMembershipTiers = 126,

    [PermissionInfo("Xoá hạng thành viên", PermissionModule.SuperAdmin, PermissionKind.Delete, null,
        "DELETE /api/v1/membership-tiers/{id}", Screen = PermissionScreens.MembershipTiers,
        Replaces = new[] { "P109" })]
    DeleteMembershipTiers = 127,

    [PermissionInfo("Xem tài khoản ngân hàng", PermissionModule.SuperAdmin, PermissionKind.View, null,
        "GET /api/v1/bank-accounts", Screen = PermissionScreens.BankAccounts,
        Replaces = new[] { "P110" })]
    ViewBankAccounts = 128,

    [PermissionInfo("Xem cấu hình loại thu/chi", PermissionModule.Revenue, PermissionKind.View, "/admin/revenue/settings",
        "GET /api/v1/RevenueExpenseTypes, GET /api/v1/RevenueExpenseTypes/config", Screen = PermissionScreens.RevenueConfig,
        Replaces = new[] { "P114" })]
    ViewRevenueConfig = 129,

    [PermissionInfo("Sửa cấu hình loại thu/chi", PermissionModule.Revenue, PermissionKind.Update, null,
        "POST /api/v1/RevenueExpenseTypes, PUT /api/v1/RevenueExpenseTypes/{id}, PUT /api/v1/RevenueExpenseTypes/assign, PUT /api/v1/RevenueExpenseTypes/config", Screen = PermissionScreens.RevenueConfig,
        Replaces = new[] { "P114" })]
    UpdateRevenueConfig = 130,

    [PermissionInfo("Xoá cấu hình loại thu/chi", PermissionModule.Revenue, PermissionKind.Delete, null,
        "DELETE /api/v1/RevenueExpenseTypes/{id}", Screen = PermissionScreens.RevenueConfig,
        Replaces = new[] { "P114" })]
    DeleteRevenueConfig = 131,

    // ===== Quyền khôi phục theo cặp ViewRestore<X> + Restore<X> cho mọi màn có xoá mềm =====
    // Cặp quyền này gác cả endpoint "danh sách đã xoá" lẫn endpoint "khôi phục" của màn hình,
    // theo đúng mẫu CollaboratorsController ([HasPermission(ViewRestore<X>, Restore<X>)], kèm mã cũ khi cần).

    [PermissionInfo("Xem đối tác đã xoá", PermissionModule.Partner, PermissionKind.View, "/admin/partner",
        "GET /api/v1/partners/deleted", Screen = PermissionScreens.Partners)]
    ViewRestorePartner = 132,

    [PermissionInfo("Xem offer đã xoá", PermissionModule.Purchase, PermissionKind.View, "/admin/offers",
        "GET /api/v1/offerrequests/deleted", Screen = PermissionScreens.Offers)]
    ViewRestoreOfferRequest = 133,

    [PermissionInfo("Xem bài đăng đã xoá", PermissionModule.Community, PermissionKind.View, "/admin/social-posts",
        "GET /api/v1/social/posts/deleted", Screen = PermissionScreens.SocialPosts)]
    ViewRestoreSocialPost = 134,

    [PermissionInfo("Xem yêu cầu mua chung đã xoá", PermissionModule.Purchase, PermissionKind.View, "/admin/group-buying",
        "GET /api/v1/groupbuyingrequests/deleted", Screen = PermissionScreens.GroupBuying)]
    ViewRestoreGroupBuyingRequest = 135,

    [PermissionInfo("Khôi phục yêu cầu mua chung", PermissionModule.Purchase, PermissionKind.Delete, null,
        "POST /api/v1/groupbuyingrequests/{id}/restore", Screen = PermissionScreens.GroupBuying)]
    RestoreGroupBuyingRequest = 136,

    [PermissionInfo("Xem nhóm đã xoá", PermissionModule.Group, PermissionKind.View, "/admin/groups",
        "GET /api/v1/businessgroups/deleted", Screen = PermissionScreens.Groups)]
    ViewRestoreGroup = 137,

    [PermissionInfo("Khôi phục nhóm", PermissionModule.Group, PermissionKind.Delete, null,
        "POST /api/v1/businessgroups/{id}/restore", Screen = PermissionScreens.Groups)]
    RestoreGroup = 138,

    [PermissionInfo("Xem cấu hình hoa hồng đã xoá", PermissionModule.SuperAdmin, PermissionKind.View, "/admin/settings",
        "GET /api/v1/commissions/deleted", Screen = PermissionScreens.CommissionConfig)]
    ViewRestoreCommissionConfig = 139,

    [PermissionInfo("Khôi phục cấu hình hoa hồng", PermissionModule.SuperAdmin, PermissionKind.Delete, null,
        "POST /api/v1/commissions/{id}/restore", Screen = PermissionScreens.CommissionConfig)]
    RestoreCommissionConfig = 140,

    [PermissionInfo("Xem hạng thành viên đã xoá", PermissionModule.SuperAdmin, PermissionKind.View, "/admin/finance",
        "GET /api/v1/membership-tiers/deleted", Screen = PermissionScreens.MembershipTiers)]
    ViewRestoreMembershipTier = 141,

    [PermissionInfo("Khôi phục hạng thành viên", PermissionModule.SuperAdmin, PermissionKind.Delete, null,
        "POST /api/v1/membership-tiers/{id}/restore", Screen = PermissionScreens.MembershipTiers)]
    RestoreMembershipTier = 142,

    [PermissionInfo("Xem cấu hình loại thu/chi đã xoá", PermissionModule.SuperAdmin, PermissionKind.View, "/admin/revenue/settings",
        "GET /api/v1/RevenueExpenseTypes/deleted", Screen = PermissionScreens.RevenueConfig)]
    ViewRestoreRevenueConfig = 143,

    [PermissionInfo("Khôi phục cấu hình loại thu/chi", PermissionModule.SuperAdmin, PermissionKind.Delete, null,
        "POST /api/v1/RevenueExpenseTypes/{id}/restore", Screen = PermissionScreens.RevenueConfig)]
    RestoreRevenueConfig = 144,

    // ===== Yêu cầu mua hàng: đủ bộ xoá mềm / danh sách đã xoá / khôi phục =====

    [PermissionInfo("Xoá yêu cầu mua hàng", PermissionModule.Purchase, PermissionKind.Delete, null,
        "DELETE /api/v1/purchaserequests/{id}", Screen = PermissionScreens.PurchaseRequests)]
    DeletePurchaseRequest = 145,

    [PermissionInfo("Xem yêu cầu mua hàng đã xoá", PermissionModule.Purchase, PermissionKind.View, "/admin/purchase-requests",
        "GET /api/v1/purchaserequests/deleted", Screen = PermissionScreens.PurchaseRequests)]
    ViewRestorePurchaseRequest = 146,

    [PermissionInfo("Khôi phục yêu cầu mua hàng", PermissionModule.Purchase, PermissionKind.Delete, null,
        "POST /api/v1/purchaserequests/{id}/restore", Screen = PermissionScreens.PurchaseRequests)]
    RestorePurchaseRequest = 147,
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
