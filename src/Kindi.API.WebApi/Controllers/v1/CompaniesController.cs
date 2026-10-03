using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.Requests;
using Kindi.API.Application.DTOs.Responses;
using Kindi.API.Application.Resources;
using Kindi.API.WebApi;
using Kindi.API.WebApi.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Kindi.API.Shared.Constants;

namespace Kindi.API.WebApi.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class CompaniesController : ApiControllerBase
{
    private readonly ICompanyService _companyService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CompaniesController(ICompanyService companyService, IStringLocalizer<SharedResource> localizer)
    {
        _companyService = companyService;
        _localizer = localizer;
    }

    [HttpGet]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<IActionResult> GetList([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _companyService.GetPagedAsync(pageNumber, pageSize, search);
        return OkPaged(result, _localizer["Success"]);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<IActionResult> GetDetail(Guid id)
    {
        var result = await _companyService.GetByIdAsync(id);
        if (result == null) return NotFound(_localizer["NotFound"]);
        return Ok(result, _localizer["Success"]);
    }

    [HttpPost]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<IActionResult> Create([FromBody] CreateCompanyDto request)
    {
        var result = await _companyService.CreateAsync(request);
        return Ok(result, _localizer["Success"]);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCompanyDto request)
    {
        var result = await _companyService.UpdateAsync(id, request);
        return Ok(result, _localizer["Success"]);
    }
}
