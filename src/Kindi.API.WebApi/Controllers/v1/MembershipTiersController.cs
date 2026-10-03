using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.Resources;
using Kindi.API.Domain.Enums;
using Kindi.API.WebApi.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Kindi.API.WebApi.Controllers.v1;

/// <summary>
/// Hạng thành viên: cấu hình quyền lợi của từng hạng, xét hạng theo doanh số tích luỹ và xem hạng của chính mình.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class MembershipTiersController : ApiControllerBase
{
    private readonly IMembershipTierService _membershipTierService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public MembershipTiersController(
        IMembershipTierService membershipTierService,
        IStringLocalizer<SharedResource> localizer)
    {
        _membershipTierService = membershipTierService;
        _localizer = localizer;
    }

    /// <summary>Danh sách hạng thành viên kèm quyền lợi.</summary>
    [HttpGet]
    [HasPermission(PermissionCode.ViewPayouts)]
    public async Task<IActionResult> GetTiers([FromQuery] bool activeOnly = false)
    {
        var result = await _membershipTierService.GetTiersAsync(activeOnly);
        return Ok(result, _localizer["Membership_ListSuccess"]);
    }

    /// <summary>Thêm mới hoặc cập nhật một hạng thành viên.</summary>
    [HttpPost]
    [HasPermission(PermissionCode.ManageMembershipTiers)]
    public async Task<IActionResult> Save([FromBody] SaveMembershipTierRequest request, [FromQuery] Guid? id = null)
    {
        var result = await _membershipTierService.SaveAsync(id, request);
        return Ok(result, _localizer["Membership_SaveSuccess"]);
    }

    /// <summary>Xoá một hạng thành viên.</summary>
    [HttpDelete("{id:guid}")]
    [HasPermission(PermissionCode.ManageMembershipTiers)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _membershipTierService.DeleteAsync(id);
        return Ok(new { Id = id }, _localizer["Membership_DeleteSuccess"]);
    }

    /// <summary>Xét lại hạng thành viên theo doanh số tích luỹ.</summary>
    [HttpPost("evaluate")]
    [HasPermission(PermissionCode.ManageMembershipTiers)]
    public async Task<IActionResult> Evaluate([FromBody] EvaluateMembershipRequest request)
    {
        var count = await _membershipTierService.EvaluateAsync(request);
        return Ok(new { Evaluated = count }, _localizer["Membership_EvaluateSuccess"]);
    }

    /// <summary>Hạng thành viên và quyền lợi hiện tại của chính người gọi.</summary>
    [HttpGet("me")]
    [HasPermission(PermissionCode.ViewMyCommission)]
    public async Task<IActionResult> GetMine()
    {
        var result = await _membershipTierService.GetMineAsync();
        return Ok(result, _localizer["Membership_MySuccess"]);
    }
}
