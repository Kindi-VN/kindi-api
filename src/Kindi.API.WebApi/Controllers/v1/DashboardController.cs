using Kindi.API.Application.Features.Dashboard.Queries;
using Kindi.API.Application.Resources;
using Kindi.API.WebApi;
using MediatR;
using Kindi.API.WebApi.Authorization;
using Kindi.API.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Kindi.API.Shared.Constants;

namespace Kindi.API.WebApi.Controllers.Admin;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin")]
[Authorize(Roles = RoleConstants.Admin)]
[ApiController]
public class DashboardController : ApiControllerBase
{
	private readonly IMediator _mediator;
	private readonly IStringLocalizer<SharedResource> _localizer;

	public DashboardController(IMediator mediator, IStringLocalizer<SharedResource> localizer)
	{
		_mediator = mediator;
		_localizer = localizer;
	}

	[HasPermission(PermissionCode.ViewDashboard)]
	[HttpGet]
	public async Task<IActionResult> GetDashboard()
	{
		var stats = await _mediator.Send(new GetDashboardQuery());
		return Ok(stats, _localizer["DashboardSuccess"]);
	}

	[HasPermission(PermissionCode.ViewCrmDashboard)]
	[HttpGet("crm")]
	public async Task<IActionResult> GetCrmDashboard()
	{
		var stats = await _mediator.Send(new GetCrmDashboardQuery());
		return Ok(stats, _localizer["CrmDashboardSuccess"]);
	}
}