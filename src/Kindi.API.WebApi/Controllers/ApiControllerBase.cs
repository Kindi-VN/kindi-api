using Kindi.API.Domain.Models;
using Kindi.API.Shared.Errors;
using Kindi.API.WebApi.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Kindi.API.WebApi.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
	protected IActionResult Ok<T>(T data, string message = "Success")
	{
		return base.Ok(ApiResponse<T>.Ok(data, message));
	}

	protected IActionResult BadRequest(string message, List<string>? errors = null)
	{
		return base.BadRequest(ApiResponse<object>.Fail(message, errors, ErrorStatus.WrongRequest));
	}

	protected IActionResult NotFound(string message = "Resource not found")
	{
		return base.NotFound(ApiResponse<object>.Fail(message, status: ErrorStatus.NotFound));
	}

	protected IActionResult NotFound(string message, List<string>? errors = null)
	{
		return base.NotFound(ApiResponse<object>.Fail(message, errors, ErrorStatus.NotFound));
	}

	protected IActionResult Created<T>(string location, T data, string message = "Created successfully")
	{
		return base.Created(location, ApiResponse<T>.Ok(data, message));
	}

	protected new IActionResult NoContent()
	{
		return base.NoContent();
	}

	protected IActionResult Unauthorized(string message, List<string>? errors = null)
	{
		return base.Unauthorized(ApiResponse<object>.Fail(message, errors, ErrorStatus.Unauthorized));
	}
	protected IActionResult OkPaged<T>(PagedList<T> pagedData, string message = "Success")
	{
		return base.Ok(PagedResponse<T>.Ok(pagedData, message));
	}
}
