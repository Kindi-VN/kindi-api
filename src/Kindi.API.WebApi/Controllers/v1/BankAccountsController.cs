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
/// Thông tin ngân hàng nhận giải ngân hoa hồng: thành viên tự khai báo thông tin của mình,
/// quản trị viên xem danh sách và ghi nhận xác minh.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class BankAccountsController : ApiControllerBase
{
    private readonly IBankAccountService _bankAccountService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public BankAccountsController(
        IBankAccountService bankAccountService,
        IStringLocalizer<SharedResource> localizer)
    {
        _bankAccountService = bankAccountService;
        _localizer = localizer;
    }

    /// <summary>Thông tin ngân hàng của chính người gọi.</summary>
    [HttpGet("me")]
    [HasPermission(PermissionCode.UpdateMyBankAccount)]
    public async Task<IActionResult> GetMine()
    {
        var result = await _bankAccountService.GetMineAsync();
        return Ok(result, _localizer["BankAccount_ListSuccess"]);
    }

    /// <summary>Lưu thông tin ngân hàng của chính người gọi.</summary>
    [HttpPut("me")]
    [HasPermission(PermissionCode.UpdateMyBankAccount)]
    public async Task<IActionResult> SaveMine([FromBody] SaveBankAccountRequest request)
    {
        var result = await _bankAccountService.SaveMineAsync(request);
        return Ok(result, _localizer["BankAccount_SavedSuccess"]);
    }

    /// <summary>Tạo mã đối chiếu chuyển khoản để xác minh thông tin ngân hàng của chính người gọi.</summary>
    [HttpPost("me/verification-code")]
    [HasPermission(PermissionCode.UpdateMyBankAccount)]
    public async Task<IActionResult> IssueVerificationCode()
    {
        var result = await _bankAccountService.IssueVerificationCodeAsync();
        return Ok(result, _localizer["BankAccount_VerificationCodeIssued"]);
    }

    /// <summary>Danh sách thông tin ngân hàng để xác minh.</summary>
    [HttpGet]
    [HasPermission(PermissionCode.ViewBankAccounts, PermissionCode.VerifyBankAccounts)]
    public async Task<IActionResult> GetPaged([FromQuery] BankAccountQueryDto query)
    {
        var result = await _bankAccountService.GetPagedAsync(query);
        return OkPaged(result, _localizer["BankAccount_ListSuccess"]);
    }

    /// <summary>Ghi nhận xác minh thông tin ngân hàng của một tài khoản.</summary>
    [HttpPut("{userId:guid}/verification")]
    [HasPermission(PermissionCode.VerifyBankAccounts)]
    public async Task<IActionResult> Verify(Guid userId, [FromBody] VerifyBankAccountRequest request)
    {
        var result = await _bankAccountService.VerifyAsync(userId, request);
        return Ok(result, _localizer["BankAccount_VerifiedSuccess"]);
    }
}
