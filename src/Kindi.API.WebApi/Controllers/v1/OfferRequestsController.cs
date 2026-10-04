using AutoMapper;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.Features.OfferRequests.Commands;
using Kindi.API.Application.Features.OfferRequests.Queries;
using Kindi.API.Application.Resources;
using MediatR;
using Kindi.API.WebApi.Authorization;
using Kindi.API.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Kindi.API.Shared.Constants;

namespace Kindi.API.WebApi.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class OfferRequestsController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public OfferRequestsController(
        IMediator mediator,
        IMapper mapper,
        IStringLocalizer<SharedResource> localizer)
    {
        _mediator = mediator;
        _mapper = mapper;
        _localizer = localizer;
    }

    /// <summary>
    /// Tạo yêu cầu nhận offer (Public - không cần đăng nhập)
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> CreateAsync([FromBody] CreateOfferRequestDto request)
    {
        var command = _mapper.Map<CreateOfferRequestCommand>(request);
        var response = await _mediator.Send(command);
        return Ok(response, _localizer["OfferRequest_CreateSuccess"]);
    }

    /// <summary>
    /// Lấy danh sách yêu cầu nhận offer (admin thấy tất cả, người dùng thường chỉ thấy yêu cầu của chính mình)
    /// </summary>
    /// <remarks>
    /// Tham số <c>query.searchField</c> (tuỳ chọn) giới hạn tìm kiếm vào ĐÚNG một trường; bỏ trống thì tìm mọi trường.
    /// Các giá trị hợp lệ: <c>productName</c>, <c>code</c>, <c>recordReferrerCode</c>, <c>recordReferrerName</c>, <c>accountReferrerName</c>, <c>customerName</c>, <c>customerPhone</c>, <c>customerEmail</c>.
    /// </remarks>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetList([FromQuery] OfferRequestQueryDto query)
    {
        var request = _mapper.Map<GetOfferRequestsQuery>(query);
        var result = await _mediator.Send(request);
        return OkPaged(result, _localizer["OfferRequestListRetrievedSuccess"]);
    }

    /// <summary>
    /// Lấy chi tiết yêu cầu nhận offer theo ID (Chỉ Admin)
    /// </summary>
    [HasPermission(PermissionCode.ViewOfferRequests)]
    [HttpGet("{id:guid}")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var response = await _mediator.Send(new GetOfferRequestByIdQuery { Id = id });
        if (response == null)
        {
            return NotFound(_localizer["NotFound"],
                new List<string> { string.Format(_localizer["EntityNotFound"], "OfferRequest", id) });
        }
        return Ok(response);
    }

    /// <summary>
    /// Cập nhật trạng thái yêu cầu nhận offer (Chỉ Admin)
    /// </summary>
    [HasPermission(PermissionCode.UpdateOfferRequestStatus)]
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOfferRequestStatusDto dto)
    {
        var response = await _mediator.Send(new UpdateOfferRequestStatusCommand
        {
            Id = id,
            Status = dto.Status
        });
        return Ok(response, _localizer["UpdateStatusSuccess"]);
    }

    /// <summary>
    /// Xóa mềm yêu cầu nhận offer (Chỉ Admin)
    /// </summary>
    [HasPermission(PermissionCode.DeleteOfferRequest)]
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var response = await _mediator.Send(new DeleteOfferRequestCommand { Id = id });
        return Ok(response, _localizer["OfferRequest_DeleteSuccess"]);
    }

    /// <summary>
    /// Danh sách yêu cầu nhận offer đã xóa mềm (Chỉ Admin).
    /// </summary>
    [HasPermission(PermissionCode.ViewRestoreOfferRequest, PermissionCode.RestoreOfferRequest, PermissionCode.DeleteOfferRequest)]
    [HttpGet("deleted")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<IActionResult> GetDeleted([FromQuery] OfferRequestQueryDto query)
    {
        var request = _mapper.Map<GetOfferRequestsQuery>(query);
        request.IncludeDeleted = true;
        request.MineOnly = false;
        var result = await _mediator.Send(request);
        return OkPaged(result, _localizer["OfferRequestListRetrievedSuccess"]);
    }

    /// <summary>
    /// Khôi phục yêu cầu nhận offer đã xóa (Chỉ Admin)
    /// </summary>
    [HasPermission(PermissionCode.ViewRestoreOfferRequest, PermissionCode.RestoreOfferRequest, PermissionCode.DeleteOfferRequest)]
    [HttpPost("{id:guid}/restore")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<IActionResult> Restore(Guid id)
    {
        var response = await _mediator.Send(new RestoreOfferRequestCommand { Id = id });
        return Ok(response, _localizer["RestoreSuccess"]);
    }
}