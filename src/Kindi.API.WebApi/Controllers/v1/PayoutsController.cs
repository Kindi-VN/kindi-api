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
/// Giải ngân hoa hồng: ví hoa hồng của thành viên, yêu cầu rút sớm (có phí rút sớm theo hạng và cấu hình chung),
/// danh sách chi trả và cấu hình phí rút sớm.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class PayoutsController : ApiControllerBase
{
    private readonly IPayoutService _payoutService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public PayoutsController(
        IPayoutService payoutService,
        IStringLocalizer<SharedResource> localizer)
    {
        _payoutService = payoutService;
        _localizer = localizer;
    }

    /// <summary>Danh sách chi trả hoa hồng (theo kỳ và rút sớm).</summary>
    [HttpGet]
    [HasPermission(PermissionCode.ViewPayouts)]
    public async Task<IActionResult> GetPaged([FromQuery] PayoutQueryDto query)
    {
        var result = await _payoutService.GetPagedAsync(query);
        return OkPaged(result, _localizer["Payout_ListSuccess"]);
    }

    /// <summary>
    /// Danh sách chi trả hoa hồng của CHÍNH người gọi (màn hoa hồng của tôi).
    /// Luôn ép UserId theo tài khoản đang đăng nhập nên không cần quyền xem chi trả toàn hệ thống.
    /// </summary>
    [HttpGet("my")]
    [HasPermission(PermissionCode.ViewMyCommission)]
    public async Task<IActionResult> GetMyPaged([FromQuery] PayoutQueryDto query)
    {
        var result = await _payoutService.GetMyPagedAsync(query);
        return OkPaged(result, _localizer["Payout_ListSuccess"]);
    }

    /// <summary>Ví hoa hồng của chính người gọi: số dư có thể rút, phí rút sớm và hạn mức còn lại.</summary>
    [HttpGet("me")]
    [HasPermission(PermissionCode.ViewMyCommission)]
    public async Task<IActionResult> GetWallet()
    {
        var result = await _payoutService.GetWalletAsync();
        return Ok(result, _localizer["Payout_MyWalletSuccess"]);
    }

    /// <summary>Gửi yêu cầu rút hoa hồng sớm.</summary>
    [HttpPost("withdrawals")]
    [HasPermission(PermissionCode.RequestCommissionWithdrawal)]
    public async Task<IActionResult> CreateWithdrawal([FromBody] CreateWithdrawalRequest request)
    {
        var result = await _payoutService.CreateWithdrawalAsync(request);
        return Ok(result, _localizer["Payout_WithdrawalSuccess"]);
    }

    /// <summary>Huỷ yêu cầu rút sớm đang chờ duyệt của chính mình.</summary>
    [HttpPost("{id:guid}/cancel")]
    [HasPermission(PermissionCode.RequestCommissionWithdrawal)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var result = await _payoutService.CancelAsync(id);
        return Ok(result, _localizer["Payout_CancelSuccess"]);
    }

    /// <summary>Duyệt một lần chi trả.</summary>
    [HttpPost("{id:guid}/approve")]
    [HasPermission(PermissionCode.ProcessPayouts)]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ProcessPayoutRequest? request)
    {
        var result = await _payoutService.ApproveAsync(id, request ?? new ProcessPayoutRequest());
        return Ok(result, _localizer["Payout_ProcessSuccess"]);
    }

    /// <summary>Từ chối một lần chi trả.</summary>
    [HttpPost("{id:guid}/reject")]
    [HasPermission(PermissionCode.ProcessPayouts)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] ProcessPayoutRequest request)
    {
        var result = await _payoutService.RejectAsync(id, request);
        return Ok(result, _localizer["Payout_ProcessSuccess"]);
    }

    /// <summary>Xác nhận đã chuyển khoản một lần chi trả.</summary>
    [HttpPost("{id:guid}/paid")]
    [HasPermission(PermissionCode.ProcessPayouts)]
    public async Task<IActionResult> MarkPaid(Guid id, [FromBody] ProcessPayoutRequest? request)
    {
        var result = await _payoutService.MarkPaidAsync(id, request ?? new ProcessPayoutRequest());
        return Ok(result, _localizer["Payout_ProcessSuccess"]);
    }

    /// <summary>Cấu hình phí rút sớm dùng chung.</summary>
    [HttpGet("settings")]
    [HasPermission(PermissionCode.ViewPayouts)]
    public async Task<IActionResult> GetSettings()
    {
        var result = await _payoutService.GetSettingsAsync();
        return Ok(result, _localizer["Payout_SettingSuccess"]);
    }

    /// <summary>Cập nhật cấu hình phí rút sớm dùng chung.</summary>
    [HttpPut("settings")]
    [HasPermission(PermissionCode.ManageMembershipTiers)]
    public async Task<IActionResult> SaveSettings([FromBody] SavePayoutSettingRequest request)
    {
        var result = await _payoutService.SaveSettingsAsync(request);
        return Ok(result, _localizer["Payout_SettingSavedSuccess"]);
    }
}
