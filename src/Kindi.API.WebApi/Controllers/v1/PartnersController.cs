// WebApi/Controllers/PartnerController.cs
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.Requests;
using Kindi.API.Application.DTOs.Responses;
using Kindi.API.Application.Resources;
using Kindi.API.WebApi;
using Kindi.API.WebApi.Responses;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Kindi.API.Shared.Constants;

namespace Kindi.API.WebApi.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class PartnersController : ApiControllerBase
{
    private readonly IPartnerService _partnerService;
    private readonly IValidator<PartnerRegisterRequest> _validator;
    private readonly IValidator<UpdatePartnerDto> _updateValidator;
    private readonly IValidator<CreatePartnerProductDto> _createProductValidator;
    private readonly IValidator<UpdatePartnerProductDto> _updateProductValidator;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public PartnersController(
        IPartnerService partnerService,
        IValidator<PartnerRegisterRequest> validator,
        IValidator<UpdatePartnerDto> updateValidator,
        IValidator<CreatePartnerProductDto> createProductValidator,
        IValidator<UpdatePartnerProductDto> updateProductValidator,
        IStringLocalizer<SharedResource> localizer)
    {
        _partnerService = partnerService;
        _validator = validator;
        _updateValidator = updateValidator;
        _createProductValidator = createProductValidator;
        _updateProductValidator = updateProductValidator;
        _localizer = localizer;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] PartnerRegisterRequest request)
    {
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = _localizer["PartnerValidationFailed"],
                Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList()
            });
        }

        var result = await _partnerService.RegisterAsync(request);
        return Ok(result, _localizer["PartnerRegisterSuccess"]);
    }

    /// <summary>
    /// Danh sách nhà cung cấp công khai (trang Nguồn cung): đối tác doanh nghiệp đã đăng ký và được duyệt.
    /// </summary>
    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicList([FromQuery] PublicPartnerQueryDto query)
    {
        var result = await _partnerService.GetPublicPagedAsync(query);
        return OkPaged(result, _localizer["Success"]);
    }

    /// <summary>
    /// Lấy danh sách đối tác phân trang.
    /// Truyền <c>isDeleted=true</c> để lấy danh sách đối tác đã xóa mềm.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<IActionResult> GetList([FromQuery] PartnerFilterRequest filter)
    {
        var result = await _partnerService.GetPagedAsync(filter);
        return OkPaged(result, _localizer["Success"]);
    }

    [Authorize(Roles = RoleConstants.Admin)]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetDetail(Guid id)
    {
        var result = await _partnerService.GetDetailAsync(id);
        if (result == null)
            return NotFound(_localizer["Partner_NotFound"]);

        return Ok(result, _localizer["Success"]);
    }

    [HttpPost("{id}/approve")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<IActionResult> Approve(Guid id)
    {
        var result = await _partnerService.ApproveAsync(id);
        return Ok(result, _localizer["Partner_ApproveSuccess"]);
    }

    [Authorize(Roles = RoleConstants.Admin)]
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(Guid id)
    {
        var result = await _partnerService.RejectAsync(id);
        return Ok(result, _localizer["Partner_RejectSuccess"]);
    }

    [Authorize(Roles = RoleConstants.Admin)]
    [HttpPost("{id}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        var result = await _partnerService.ActivateAsync(id);
        return Ok(result, _localizer["Partner_ActivateSuccess"]);
    }

    /// <summary>
    /// Cập nhật thông tin đối tác (partial update — field null giữ nguyên)
    /// </summary>
    [Authorize(Roles = RoleConstants.Admin)]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePartnerDto request)
    {
        var validationResult = await _updateValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = _localizer["PartnerValidationFailed"],
                Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList()
            });
        }

        var result = await _partnerService.UpdateAsync(id, request);
        return Ok(result, _localizer["Partner_UpdateSuccess"]);
    }

    /// <summary>
    /// Danh sách đối tác đã xóa mềm
    /// </summary>
    //[Authorize(Roles = RoleConstants.Admin)]
    [HttpGet("deleted")]
    public async Task<IActionResult> GetDeleted(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var result = await _partnerService.GetPagedDeletedAsync(pageNumber, pageSize, search);
        return OkPaged(result, _localizer["Success"]);
    }

    /// <summary>
    /// Xóa mềm đối tác
    /// </summary>
    [Authorize(Roles = RoleConstants.Admin)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _partnerService.DeleteAsync(id);
        // .Value là bắt buộc: LocalizedString nằm trong object ẩn danh sẽ bị
        // System.Text.Json serialize thành { name, value, resourceNotFound }.
        return Ok(new { message = _localizer["Partner_DeleteSuccess"].Value });
    }

    /// <summary>
    /// Khôi phục đối tác đã xóa
    /// </summary>
    [Authorize(Roles = RoleConstants.Admin)]
    [HttpPost("{id}/restore")]
    public async Task<IActionResult> Restore(Guid id)
    {
        var result = await _partnerService.RestoreAsync(id);
        return Ok(result, _localizer["Partner_RestoreSuccess"]);
    }

    // ===== Sản phẩm / dịch vụ của đối tác =====
    // Quản lý riêng thay vì gửi kèm trong PUT /partners/{id} — nhờ vậy sửa một sản phẩm
    // không làm xóa mềm rồi tạo lại toàn bộ danh sách (giữ nguyên Id và mã PRDP).

    /// <summary>
    /// Thêm sản phẩm/dịch vụ cho đối tác
    /// </summary>
    [Authorize(Roles = RoleConstants.Admin)]
    [HttpPost("{id}/products")]
    public async Task<IActionResult> AddProduct(Guid id, [FromBody] CreatePartnerProductDto request)
    {
        var validationResult = await _createProductValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return ValidationFailed(validationResult);

        var result = await _partnerService.AddProductAsync(id, request);
        return Ok(result, _localizer["Partner_ProductAddSuccess"]);
    }

    /// <summary>
    /// Cập nhật sản phẩm/dịch vụ của đối tác (partial update — field null giữ nguyên)
    /// </summary>
    [Authorize(Roles = RoleConstants.Admin)]
    [HttpPut("{id}/products/{productId}")]
    public async Task<IActionResult> UpdateProduct(
        Guid id,
        Guid productId,
        [FromBody] UpdatePartnerProductDto request)
    {
        var validationResult = await _updateProductValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return ValidationFailed(validationResult);

        var result = await _partnerService.UpdateProductAsync(id, productId, request);
        return Ok(result, _localizer["Partner_ProductUpdateSuccess"]);
    }

    /// <summary>
    /// Xóa mềm sản phẩm/dịch vụ của đối tác
    /// </summary>
    [Authorize(Roles = RoleConstants.Admin)]
    [HttpDelete("{id}/products/{productId}")]
    public async Task<IActionResult> DeleteProduct(Guid id, Guid productId)
    {
        await _partnerService.DeleteProductAsync(id, productId);
        return Ok(new { message = _localizer["Partner_ProductDeleteSuccess"].Value });
    }

    private IActionResult ValidationFailed(FluentValidation.Results.ValidationResult result)
        => BadRequest(new ApiResponse<object>
        {
            Success = false,
            Message = _localizer["PartnerValidationFailed"],
            Errors = result.Errors.Select(e => e.ErrorMessage).ToList()
        });
}