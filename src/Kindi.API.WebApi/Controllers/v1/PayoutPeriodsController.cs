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
/// Kỳ giải ngân hoa hồng theo tháng: mặc định chốt sổ cuối tháng và chi trả đầu tháng sau.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class PayoutPeriodsController : ApiControllerBase
{
    private readonly IPayoutService _payoutService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public PayoutPeriodsController(
        IPayoutService payoutService,
        IStringLocalizer<SharedResource> localizer)
    {
        _payoutService = payoutService;
        _localizer = localizer;
    }

    /// <summary>Danh sách kỳ giải ngân.</summary>
    [HttpGet]
    [HasPermission(PermissionCode.ViewPayouts)]
    public async Task<IActionResult> GetPaged([FromQuery] PayoutPeriodQueryDto query)
    {
        var result = await _payoutService.GetPeriodsAsync(query);
        return OkPaged(result, _localizer["Payout_PeriodListSuccess"]);
    }

    /// <summary>Mở kỳ giải ngân của một tháng (đã có thì trả về kỳ đang có).</summary>
    [HttpPost]
    [HasPermission(PermissionCode.ProcessPayouts)]
    public async Task<IActionResult> Open([FromBody] CreatePayoutPeriodRequest request)
    {
        var result = await _payoutService.OpenPeriodAsync(request);
        return Ok(result, _localizer["Payout_PeriodSavedSuccess"]);
    }

    /// <summary>Chốt sổ một kỳ: ghi nhận hoa hồng của từng thành viên thành các lần chi trả.</summary>
    [HttpPost("{id:guid}/close")]
    [HasPermission(PermissionCode.ProcessPayouts)]
    public async Task<IActionResult> Close(Guid id)
    {
        var result = await _payoutService.ClosePeriodAsync(id);
        return Ok(result, _localizer["Payout_PeriodClosedSuccess"]);
    }

    /// <summary>Xác nhận đã chi trả toàn bộ các lần chi trả của một kỳ.</summary>
    [HttpPost("{id:guid}/pay")]
    [HasPermission(PermissionCode.ProcessPayouts)]
    public async Task<IActionResult> Pay(Guid id)
    {
        var result = await _payoutService.PayPeriodAsync(id);
        return Ok(result, _localizer["Payout_PeriodPaidSuccess"]);
    }
}
