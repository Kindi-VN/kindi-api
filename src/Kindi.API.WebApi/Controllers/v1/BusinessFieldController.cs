using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.Requests;
using Kindi.API.WebApi.Authorization;
using Kindi.API.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kindi.API.WebApi.Controllers.v1;

[ApiVersion("1.0")]
[ApiController]
// Route dạng kebab-case số nhiều (client gọi /business-fields/active).
[Route("api/v{version:apiVersion}/business-fields")]
public class BusinessFieldController : ApiControllerBase
{
    private readonly IBusinessFieldService _businessFieldService;

    public BusinessFieldController(IBusinessFieldService businessFieldService)
    {
        _businessFieldService = businessFieldService;
    }

    /// <summary>
    /// Lấy danh sách lĩnh vực hoạt động đang hoạt động
    /// </summary>
    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActiveFields()
    {
        var fields = await _businessFieldService.GetActiveFieldsAsync();
        return Ok(fields);
    }

    /// <summary>
    /// Danh sách lĩnh vực cho màn quản trị (gồm cả lĩnh vực đang tắt) kèm số công ty/tài khoản.
    /// </summary>
    [HttpGet]
    [HasPermission(PermissionCode.ViewCompanies)]
    public async Task<IActionResult> GetAllForAdmin()
    {
        var fields = await _businessFieldService.GetAllForAdminAsync();
        return Ok(fields);
    }

    /// <summary>
    /// Công ty và tài khoản thuộc một lĩnh vực.
    /// </summary>
    [HttpGet("{id}/related")]
    [HasPermission(PermissionCode.ViewCompanies)]
    public async Task<IActionResult> GetRelated(Guid id)
    {
        var related = await _businessFieldService.GetRelatedAsync(id);
        return Ok(related);
    }

    /// <summary>
    /// Thêm lĩnh vực hoạt động.
    /// </summary>
    [HttpPost]
    [HasPermission(PermissionCode.ManageCompanies)]
    public async Task<IActionResult> Create([FromBody] CreateBusinessFieldRequest request)
    {
        var field = await _businessFieldService.CreateAsync(request);
        return Ok(field);
    }

    /// <summary>
    /// Sửa lĩnh vực hoạt động (đổi tên, tên gọi khác, bật/tắt).
    /// </summary>
    [HttpPut("{id}")]
    [HasPermission(PermissionCode.ManageCompanies)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBusinessFieldRequest request)
    {
        var field = await _businessFieldService.UpdateAsync(id, request);
        return Ok(field);
    }

    /// <summary>
    /// Xoá lĩnh vực hoạt động (chặn nếu đang được công ty/hồ sơ sử dụng).
    /// </summary>
    [HttpDelete("{id}")]
    [HasPermission(PermissionCode.ManageCompanies)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _businessFieldService.DeleteAsync(id);
        return NoContent();
    }
}
