using AutoMapper;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Application.Features.PurchaseRequests.Commands;
using Kindi.API.Application.Features.PurchaseRequests.Queries;
using Kindi.API.Application.Resources;
using Kindi.API.WebApi.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Kindi.API.WebApi.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class PurchaseRequestsController : ApiControllerBase
{
	private readonly IMediator _mediator;
	private readonly IMapper _mapper;
	private readonly IPurchaseRequestService _service;
	private readonly IStringLocalizer<SharedResource> _localizer;

	public PurchaseRequestsController(IPurchaseRequestService service, IMediator mediator, IMapper mapper, IStringLocalizer<SharedResource> localizer)
	{
		_mediator = mediator;
		_mapper = mapper;
		_localizer = localizer;
		_service = service;
	}

    [AllowAnonymous]
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PurchaseRequestResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> CreateAsync([FromBody] CreatePurchaseRequestDto request)
    {
        var command = _mapper.Map<CreatePurchaseRequestCommand>(request);
        var response = await _mediator.Send(command);
        return Ok(response, _localizer["PurchaseRequest_CreatePurchaseRequestSuccess"]);
    }

    [Authorize(Roles = "Admin")]
	[HttpGet("{id:guid}")]
	public async Task<IActionResult> GetByIdAsync(Guid id)
	{
		var response = await _mediator.Send(new GetPurchaseRequestByIdQuery { Id = id });
		if (response == null)
			return NotFound(_localizer["NotFound"],
				new List<string> { string.Format(_localizer["EntityNotFound"], "PurchaseRequest", id) });
		return Ok(response);
	}

	/// <summary>
	/// Lấy danh sách yêu cầu mua (admin thấy tất cả, người dùng thường chỉ thấy yêu cầu của chính mình)
	/// </summary>
	[Authorize]
	[HttpGet]
	public async Task<IActionResult> GetListAsync([FromQuery] PurchaseRequestQueryDto query)
	{
		var result = await _service.GetPagedAsync(query);
		return OkPaged(result, _localizer["PurchaseRequestsRetrievedSuccess"]);
	}

	[Authorize(Roles = "Admin")]
	[HttpPatch("{id:guid}/status")]
	public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdatePurchaseRequestStatusDto dto)
	{
		var result = await _service.UpdateStatusAsync(id, dto);
		return Ok(result, _localizer["UpdateStatusSuccess"]);
	}

	[Authorize(Roles = "Admin")]
	[HttpGet("export")]
	public async Task<IActionResult> ExportAsync([FromQuery] ExportPurchaseRequestsQuery query)
	{
		var bytes = await _mediator.Send(query);
		var fileName = $"purchase_requests_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
		return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
	}
}