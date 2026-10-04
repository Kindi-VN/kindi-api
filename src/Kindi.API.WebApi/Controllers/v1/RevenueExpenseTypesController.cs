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
/// Cấu hình loại chi phí dùng chung khi khai doanh thu giao dịch: quản trị viên đặt tên, thứ tự và gắn
/// loại chi phí cho từng loại giao dịch (không gắn gì là loại mặc định). Kèm hai cột cấu hình thuế
/// doanh thu để trang cấu hình không phải mượn API cài đặt hệ thống.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class RevenueExpenseTypesController : ApiControllerBase
{
    private readonly IRevenueExpenseTypeService _expenseTypeService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public RevenueExpenseTypesController(
        IRevenueExpenseTypeService expenseTypeService,
        IStringLocalizer<SharedResource> localizer)
    {
        _expenseTypeService = expenseTypeService;
        _localizer = localizer;
    }

    /// <summary>Toàn bộ loại chi phí kèm các loại giao dịch được gắn.</summary>
    [HttpGet]
    [HasPermission(PermissionCode.ViewRevenueConfig, PermissionCode.ManageRevenueConfig)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _expenseTypeService.GetAllAsync();
        return Ok(result, _localizer["RevenueExpenseType_ListSuccess"]);
    }

    /// <summary>Tạo một loại chi phí mới.</summary>
    [HttpPost]
    [HasPermission(PermissionCode.UpdateRevenueConfig, PermissionCode.ManageRevenueConfig)]
    public async Task<IActionResult> Create([FromBody] SaveRevenueExpenseTypeRequest request)
    {
        var result = await _expenseTypeService.CreateAsync(request);
        return Ok(result, _localizer["RevenueExpenseType_Saved"]);
    }

    /// <summary>Cập nhật một loại chi phí và thay toàn bộ scope của nó.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCode.UpdateRevenueConfig, PermissionCode.ManageRevenueConfig)]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveRevenueExpenseTypeRequest request)
    {
        var result = await _expenseTypeService.UpdateAsync(id, request);
        return Ok(result, _localizer["RevenueExpenseType_Saved"]);
    }

    /// <summary>Xoá một loại chi phí cùng các scope của nó.</summary>
    [HttpDelete("{id:guid}")]
    [HasPermission(PermissionCode.DeleteRevenueConfig, PermissionCode.ManageRevenueConfig)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _expenseTypeService.DeleteAsync(id);
        return Ok(new { Id = id }, _localizer["RevenueExpenseType_Deleted"]);
    }

    /// <summary>Gán hàng loạt loại giao dịch cho một nhóm loại chi phí (rỗng = chuyển về mặc định).</summary>
    [HttpPut("assign")]
    [HasPermission(PermissionCode.UpdateRevenueConfig, PermissionCode.ManageRevenueConfig)]
    public async Task<IActionResult> Assign([FromBody] AssignRevenueExpenseTypeScopesRequest request)
    {
        await _expenseTypeService.AssignAsync(request);
        return Ok(new { Count = request.ExpenseTypeIds.Distinct().Count() }, _localizer["RevenueExpenseType_Assigned"]);
    }

    /// <summary>Hai cột cấu hình thuế doanh thu trong cài đặt chung.</summary>
    [HttpGet("config")]
    [HasPermission(PermissionCode.ViewRevenueConfig, PermissionCode.ManageRevenueConfig)]
    public async Task<IActionResult> GetConfig()
    {
        var result = await _expenseTypeService.GetConfigAsync();
        return Ok(result, _localizer["RevenueExpenseType_ConfigSuccess"]);
    }

    /// <summary>Cập nhật riêng hai cột cấu hình thuế doanh thu, giữ nguyên mọi cột khác.</summary>
    [HttpPut("config")]
    [HasPermission(PermissionCode.UpdateRevenueConfig, PermissionCode.ManageRevenueConfig)]
    public async Task<IActionResult> SaveConfig([FromBody] RevenueExpenseConfigRequest request)
    {
        var result = await _expenseTypeService.SaveConfigAsync(request);
        return Ok(result, _localizer["RevenueExpenseType_ConfigSaved"]);
    }

    /// <summary>Giải nhóm loại chi phí áp cho một loại giao dịch (màn khai doanh thu gọi).</summary>
    [HttpGet("resolve")]
    public async Task<IActionResult> Resolve([FromQuery] TransactionType type)
    {
        var result = await _expenseTypeService.ResolveAsync(type);
        return Ok(result, _localizer["RevenueExpenseType_ResolveSuccess"]);
    }
}
