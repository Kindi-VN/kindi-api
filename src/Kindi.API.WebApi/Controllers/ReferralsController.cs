using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kindi.API.WebApi.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ReferralsController : ApiControllerBase
{
    private readonly IReferralService _referralService;

    public ReferralsController(IReferralService referralService)
    {
        _referralService = referralService;
    }

    /// <summary>
    /// Ghi nhận mã chia sẻ (?ref=) trên link khách đang mở vào tài khoản đang đăng nhập.
    /// Chỉ ghi nhận lần đầu — mở link của CTV khác sau đó không ghi đè.
    /// </summary>
    [HttpPost("me")]
    public async Task<IActionResult> SaveMyReferralCode([FromBody] SaveMyReferralCodeRequest request)
    {
        var referralCode = await _referralService.AttributeToCurrentUserAsync(request.ReferralCode);
        return Ok(new { referralCode });
    }
}
