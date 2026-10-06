using Kindi.API.Application.Common.Helpers;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Application.Errors;
using Kindi.API.Application.Resources;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Shared.Constants;
using Kindi.API.Shared.Errors;
using Kindi.API.WebApi.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Kindi.API.WebApi.Controllers.Admin;

/// <summary>
/// Xoá VĨNH VIỄN bản ghi đã xoá mềm — chỉ dùng cho màn "Đã xoá" của từng nghiệp vụ
/// (màn thường chỉ có xoá mềm). Mỗi nghiệp vụ có quyền riêng.
///
/// Điều kiện xoá: danh sách dòng được chọn (ids) HOẶC khoảng ngày xoá mềm (fromDate/toDate — mốc so là
/// `UpdatedAt` vì bảng không có cột DeletedAt).
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/trash")]
[Authorize(Roles = RoleConstants.Admin)]
[ApiController]
public class TrashController : ApiControllerBase
{
    private readonly IRepository<Collaborator> _collaborators;
    private readonly IRepository<Partner> _partners;
    private readonly IRepository<PurchaseRequest> _purchaseRequests;
    private readonly IRepository<OfferRequest> _offerRequests;
    private readonly IRepository<GroupBuyingRequest> _groupBuyingRequests;
    private readonly IRepository<BusinessGroup> _groups;
    private readonly IRepository<SocialPost> _socialPosts;
    private readonly IRepository<CommissionConfig> _commissionConfigs;
    private readonly IRepository<MembershipTier> _membershipTiers;
    private readonly IRepository<RevenueExpenseType> _revenueConfigs;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public TrashController(
        IRepository<Collaborator> collaborators,
        IRepository<Partner> partners,
        IRepository<PurchaseRequest> purchaseRequests,
        IRepository<OfferRequest> offerRequests,
        IRepository<GroupBuyingRequest> groupBuyingRequests,
        IRepository<BusinessGroup> groups,
        IRepository<SocialPost> socialPosts,
        IRepository<CommissionConfig> commissionConfigs,
        IRepository<MembershipTier> membershipTiers,
        IRepository<RevenueExpenseType> revenueConfigs,
        IStringLocalizer<SharedResource> localizer)
    {
        _collaborators = collaborators;
        _partners = partners;
        _purchaseRequests = purchaseRequests;
        _offerRequests = offerRequests;
        _groupBuyingRequests = groupBuyingRequests;
        _groups = groups;
        _socialPosts = socialPosts;
        _commissionConfigs = commissionConfigs;
        _membershipTiers = membershipTiers;
        _revenueConfigs = revenueConfigs;
        _localizer = localizer;
    }

    /// <summary>Xoá vĩnh viễn hồ sơ CTV đã xoá mềm.</summary>
    [HttpDelete("collaborators")]
    [HasPermission(PermissionCode.PurgeCollaborator)]
    public Task<IActionResult> PurgeCollaborators([FromBody] PurgeRequest request)
        => PurgeAsync(_collaborators, request, "Collaborator");

    /// <summary>Xoá vĩnh viễn đối tác đã xoá mềm.</summary>
    [HttpDelete("partners")]
    [HasPermission(PermissionCode.PurgePartner)]
    public Task<IActionResult> PurgePartners([FromBody] PurgeRequest request)
        => PurgeAsync(_partners, request, "Partner");

    /// <summary>Xoá vĩnh viễn yêu cầu mua hàng đã xoá mềm.</summary>
    [HttpDelete("purchase-requests")]
    [HasPermission(PermissionCode.PurgePurchaseRequest)]
    public Task<IActionResult> PurgePurchaseRequests([FromBody] PurgeRequest request)
        => PurgeAsync(_purchaseRequests, request, "PurchaseRequest");

    /// <summary>Xoá vĩnh viễn offer đã xoá mềm.</summary>
    [HttpDelete("offers")]
    [HasPermission(PermissionCode.PurgeOfferRequest)]
    public Task<IActionResult> PurgeOffers([FromBody] PurgeRequest request)
        => PurgeAsync(_offerRequests, request, "OfferRequest");

    /// <summary>Xoá vĩnh viễn yêu cầu mua chung đã xoá mềm.</summary>
    [HttpDelete("group-buying")]
    [HasPermission(PermissionCode.PurgeGroupBuyingRequest)]
    public Task<IActionResult> PurgeGroupBuying([FromBody] PurgeRequest request)
        => PurgeAsync(_groupBuyingRequests, request, "GroupBuyingRequest");

    /// <summary>Xoá vĩnh viễn nhóm đã xoá mềm.</summary>
    [HttpDelete("groups")]
    [HasPermission(PermissionCode.PurgeGroup)]
    public Task<IActionResult> PurgeGroups([FromBody] PurgeRequest request)
        => PurgeAsync(_groups, request, "BusinessGroup");

    /// <summary>Xoá vĩnh viễn bài đăng đã xoá mềm.</summary>
    [HttpDelete("social-posts")]
    [HasPermission(PermissionCode.PurgeSocialPost)]
    public Task<IActionResult> PurgeSocialPosts([FromBody] PurgeRequest request)
        => PurgeAsync(_socialPosts, request, "SocialPost");

    /// <summary>Xoá vĩnh viễn cấu hình hoa hồng đã xoá mềm.</summary>
    [HttpDelete("commissions")]
    [HasPermission(PermissionCode.PurgeCommissionConfig)]
    public Task<IActionResult> PurgeCommissionConfigs([FromBody] PurgeRequest request)
        => PurgeAsync(_commissionConfigs, request, "CommissionConfig");

    /// <summary>Xoá vĩnh viễn hạng thành viên đã xoá mềm.</summary>
    [HttpDelete("membership-tiers")]
    [HasPermission(PermissionCode.PurgeMembershipTier)]
    public Task<IActionResult> PurgeMembershipTiers([FromBody] PurgeRequest request)
        => PurgeAsync(_membershipTiers, request, "MembershipTier");

    /// <summary>Xoá vĩnh viễn cấu hình loại thu/chi đã xoá mềm.</summary>
    [HttpDelete("revenue-configs")]
    [HasPermission(PermissionCode.PurgeRevenueConfig)]
    public Task<IActionResult> PurgeRevenueConfigs([FromBody] PurgeRequest request)
        => PurgeAsync(_revenueConfigs, request, "RevenueExpenseType");

    /// <summary>
    /// Thực hiện xoá vĩnh viễn cho một nghiệp vụ: chỉ lấy bản ghi ĐANG xoá mềm khớp điều kiện.
    /// Bản ghi còn dữ liệu con tham chiếu sẽ bị DB chặn (khoá ngoại) — trả lỗi nghiệp vụ rõ ràng
    /// thay vì lỗi 500.
    /// </summary>
    private async Task<IActionResult> PurgeAsync<T>(IRepository<T> repository, PurgeRequest request, string entityName)
        where T : BaseEntity
    {
        var scope = PurgeRules.Resolve(request.Ids, request.FromDate, request.ToDate);

        var rows = await PurgeRules.Apply(repository.GetQueryable(), scope).ToListAsync();
        if (rows.Count == 0)
            return Ok(new PurgeResponse(entityName, 0), _localizer["Purge_NothingDeleted"]);

        int deleted;
        try
        {
            deleted = await repository.HardDeleteRangeAsync(rows);
        }
        catch (DbUpdateException)
        {
            throw new AppException(PurgeError.HasRelatedData.WithParams(entityName));
        }

        return Ok(new PurgeResponse(entityName, deleted), _localizer["Purge_Success", deleted]);
    }
}
