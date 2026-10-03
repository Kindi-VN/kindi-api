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
    private readonly IReferralEventService _referralEventService;

    public ReferralsController(IReferralService referralService, IReferralEventService referralEventService)
    {
        _referralService = referralService;
        _referralEventService = referralEventService;
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

    /// <summary>
    /// Thống kê tình hình giới thiệu theo từng mã chia sẻ: số tài khoản được mời, số đơn mua chung,
    /// yêu cầu tìm hàng / offer, lượt vào nhóm, đăng ký đối tác.
    /// </summary>
    [HttpGet("stats")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetStats([FromQuery] ReferralStatsQueryDto query)
    {
        return OkPaged(await _referralEventService.GetStatsAsync(query));
    }

    /// <summary>Số liệu tổng hợp + số phát sinh theo ngày cho board thống kê (màn quản trị).</summary>
    [HttpGet("stats/overview")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetStatsOverview([FromQuery] ReferralStatsQueryDto query)
    {
        return Ok(await _referralEventService.GetOverviewAsync(query));
    }

    /// <summary>Danh sách phát sinh của một mã chia sẻ (màn quản trị).</summary>
    [HttpGet("stats/{referralCode}/events")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetStatsEvents([FromRoute] string referralCode, [FromQuery] ReferralEventQueryDto query)
    {
        return OkPaged(await _referralEventService.GetEventsAsync(referralCode, query));
    }

    /// <summary>
    /// Thống kê của mã chia sẻ đang đăng nhập (khu vực thành viên).
    /// Mã lấy từ token của người gọi nên không xem được số liệu của tài khoản khác.
    /// </summary>
    [HttpGet("me/stats")]
    public async Task<IActionResult> GetMyStats([FromQuery] ReferralStatsQueryDto query)
    {
        return Ok(await _referralEventService.GetMyStatsAsync(query));
    }

    /// <summary>Danh sách phát sinh của chính mã đang đăng nhập (khu vực thành viên).</summary>
    [HttpGet("me/events")]
    public async Task<IActionResult> GetMyEvents([FromQuery] ReferralEventQueryDto query)
    {
        return OkPaged(await _referralEventService.GetMyEventsAsync(query));
    }
}
