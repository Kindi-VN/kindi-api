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
/// Khai doanh thu giao dịch: quản trị viên nhập doanh thu gộp và hoa hồng từng bên, hệ thống trừ thuế,
/// trừ hoa hồng và chi phí phát sinh để ra số thực nhận; chốt xong thì khoá lại. Thống kê chỉ tính bản
/// khai đã chốt.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class RevenuesController : ApiControllerBase
{
    private readonly ITransactionRevenueService _revenueService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public RevenuesController(
        ITransactionRevenueService revenueService,
        IStringLocalizer<SharedResource> localizer)
    {
        _revenueService = revenueService;
        _localizer = localizer;
    }

    /// <summary>Danh sách bản khai doanh thu (lọc theo mã giao dịch, loại và trạng thái).</summary>
    [HttpGet]
    [HasPermission(PermissionCode.ViewTransactionRevenue)]
    public async Task<IActionResult> GetPaged([FromQuery] RevenueQueryDto query)
    {
        var result = await _revenueService.GetPagedAsync(query);
        return OkPaged(result, _localizer["Revenue_ListSuccess"]);
    }

    /// <summary>Thống kê doanh thu theo khoảng thời gian, gộp theo ngày/tuần/tháng/năm.</summary>
    [HttpGet("stats")]
    [HasPermission(PermissionCode.ViewTransactionRevenue)]
    public async Task<IActionResult> GetStats([FromQuery] RevenueStatsQueryDto query)
    {
        var result = await _revenueService.GetStatsAsync(query);
        return Ok(result, _localizer["Revenue_StatsSuccess"]);
    }

    /// <summary>Bản khai của một giao dịch; chưa khai thì trả về rỗng để màn hình mở form trắng.</summary>
    [HttpGet("by-reference")]
    [HasPermission(PermissionCode.ManageTransactionRevenue)]
    public async Task<IActionResult> GetByReference([FromQuery] TransactionType type, [FromQuery] Guid referenceId)
    {
        var result = await _revenueService.GetByReferenceAsync(type, referenceId);
        return Ok(result, _localizer["Revenue_DetailSuccess"]);
    }

    /// <summary>Một bản khai theo id.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCode.ViewTransactionRevenue)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _revenueService.GetByIdAsync(id);
        return Ok(result, _localizer["Revenue_DetailSuccess"]);
    }

    /// <summary>Lưu bản khai doanh thu của một giao dịch (tính lại toàn bộ số liệu).</summary>
    [HttpPost]
    [HasPermission(PermissionCode.ManageTransactionRevenue)]
    public async Task<IActionResult> Save([FromBody] SaveTransactionRevenueRequest request)
    {
        var result = await _revenueService.SaveAsync(request);
        return Ok(result, _localizer["Revenue_Saved"]);
    }

    /// <summary>Chốt số liệu và khoá bản khai.</summary>
    [HttpPost("{id:guid}/confirm")]
    [HasPermission(PermissionCode.ManageTransactionRevenue)]
    public async Task<IActionResult> Confirm(Guid id)
    {
        var result = await _revenueService.ConfirmAsync(id);
        return Ok(result, _localizer["Revenue_Confirmed"]);
    }
}
