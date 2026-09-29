using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Kindi.API.WebApi.Responses;
using Microsoft.Extensions.Localization;
using Kindi.API.Application.Resources;
using Kindi.API.Shared.Errors;

namespace Kindi.API.WebApi.Filters;

public class ValidationFilter : IAsyncActionFilter
{
	private readonly IStringLocalizer<SharedResource> _localizer;

	public ValidationFilter(IStringLocalizer<SharedResource> localizer)
	{
		_localizer = localizer;
	}

	public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
	{
		// Kiểm tra validate TRƯỚC khi action chạy
		if (!context.ModelState.IsValid)
		{
			var errors = context.ModelState.Values
				.SelectMany(v => v.Errors)
				.Select(e => e.ErrorMessage)
				.ToList();

			var response = ApiResponse<object>.Fail(_localizer["ValidationError"], errors, ErrorStatus.ValidationError);
			context.Result = new BadRequestObjectResult(response);
			return; // Dừng lại, không chạy action
		}

		await next();
	}
}