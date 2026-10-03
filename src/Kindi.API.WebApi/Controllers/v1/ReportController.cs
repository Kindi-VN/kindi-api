using Kindi.API.Application.Features.Reports.Queries;
using Kindi.API.Application.Resources;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Kindi.API.Shared.Constants;

namespace Kindi.API.WebApi.Controllers.Admin;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/reports")]
[Authorize(Roles = RoleConstants.Admin)]
[ApiController]
public class ReportController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ReportController(IMediator mediator, IStringLocalizer<SharedResource> localizer)
    {
        _mediator = mediator;
        _localizer = localizer;
    }

    /// <summary>
    /// Báo cáo tổng quan hệ thống (người dùng, đối tác, CTV, bài viết, yêu cầu)
    /// </summary>
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var result = await _mediator.Send(new GetReportOverviewQuery
        {
            FromDate = fromDate,
            ToDate = toDate
        });
        return Ok(result, _localizer["Report_OverviewSuccess"]);
    }

    /// <summary>
    /// Báo cáo xu hướng tăng trưởng theo ngày
    /// </summary>
    [HttpGet("trend")]
    public async Task<IActionResult> GetTrend([FromQuery] int days = 30)
    {
        var result = await _mediator.Send(new GetReportTrendQuery { Days = days });
        return Ok(result, _localizer["Report_TrendSuccess"]);
    }

    /// <summary>
    /// Báo cáo yêu cầu (mua hàng / mua nhóm / báo giá) phân theo trạng thái
    /// </summary>
    [HttpGet("requests")]
    public async Task<IActionResult> GetRequests(
        [FromQuery] string? type = null)
    {
        var result = await _mediator.Send(new GetRequestReportQuery { Type = type });
        return Ok(result, _localizer["Report_RequestsSuccess"]);
    }

    /// <summary>
    /// Báo cáo hoạt động mạng xã hội (bài viết, tương tác, bài nổi bật)
    /// </summary>
    [HttpGet("social")]
    public async Task<IActionResult> GetSocial(
        [FromQuery] int topPostCount = 10)
    {
        var result = await _mediator.Send(new GetSocialReportQuery { TopPostCount = topPostCount });
        return Ok(result, _localizer["Report_SocialSuccess"]);
    }

    /// <summary>
    /// Báo cáo thành viên (đối tác + cộng tác viên)
    /// </summary>
    [HttpGet("members")]
    public async Task<IActionResult> GetMembers()
    {
        var result = await _mediator.Send(new GetMembersReportQuery());
        return Ok(result, _localizer["Report_MembersSuccess"]);
    }
}