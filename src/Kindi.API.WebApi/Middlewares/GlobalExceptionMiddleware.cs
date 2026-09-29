using Kindi.API.Application.Common.Exceptions;
using Kindi.API.Application.Resources;
using Kindi.API.Shared.Errors;
using Kindi.API.Shared.Exceptions;
using Kindi.API.WebApi.Responses;
using Microsoft.Extensions.Localization;
using System.Net;
using System.Text.Json;

namespace Kindi.API.WebApi.Middlewares;

public class GlobalExceptionMiddleware
{
	private readonly RequestDelegate _next;
	private readonly ILogger<GlobalExceptionMiddleware> _logger;
	private readonly IStringLocalizer<SharedResource> _localizer;

	public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IStringLocalizer<SharedResource> localizer)
	{
		_next = next;
		_logger = logger;
		_localizer = localizer;
	}

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        // Lỗi nghiệp vụ có mã riêng (AppException): UI bắt theo mã chuỗi, HTTP status lấy theo loại mã lỗi.
        catch (AppException ex)
        {
            _logger.LogWarning(ex, "{Status} - Path: {Path}", ex.Error.Status, context.Request.Path);
            await WriteErrorAsync(context, ex.Error);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found - Path: {Path}, Method: {Method}",
                context.Request.Path, context.Request.Method);
            await WriteExpectedAsync(context, StatusCodes.Status404NotFound, ErrorStatus.NotFound, ex.Message);
        }
        // Các lỗi "có thể dự đoán" (vi phạm nghiệp vụ) trả 4xx kèm message đã bản địa hoá
        // thay vì 500 chung chung — trước đây chỉ NotFoundException được map.
        catch (KindiException ex)
        {
            _logger.LogWarning(ex, "{Code} - Path: {Path}", ex.StatusCode, context.Request.Path);
            await WriteExpectedAsync(context, StatusCodes.Status400BadRequest, ToStatus(ex.StatusCode), ex.Message);
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "Business rule violated - Path: {Path}", context.Request.Path);
            await WriteExpectedAsync(context, StatusCodes.Status400BadRequest, ErrorStatus.WrongRequest, ex.Message);
        }
        catch (BadRequestException ex)
        {
            _logger.LogWarning(ex, "Bad request - Path: {Path}", context.Request.Path);
            await WriteExpectedAsync(context, StatusCodes.Status400BadRequest, ErrorStatus.WrongRequest, ex.Message);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed - Path: {Path}", context.Request.Path);
            await WriteExpectedAsync(context, StatusCodes.Status400BadRequest, ErrorStatus.ValidationError, ex.Message);
        }
        catch (ConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict - Path: {Path}", context.Request.Path);
            await WriteExpectedAsync(context, StatusCodes.Status409Conflict, ErrorStatus.Conflict, ex.Message);
        }
        catch (ForbiddenException ex)
        {
            _logger.LogWarning(ex, "Forbidden - Path: {Path}", context.Request.Path);
            await WriteExpectedAsync(context, StatusCodes.Status403Forbidden, ErrorStatus.Forbidden, ex.Message);
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogWarning(ex, "Unauthorized - Path: {Path}", context.Request.Path);
            await WriteExpectedAsync(context, StatusCodes.Status401Unauthorized, ErrorStatus.Unauthorized, ex.Message);
        }
        catch (Exception ex)
        {
            // Log đầy đủ thông tin
            _logger.LogError(ex, "❌ Unhandled exception - Path: {Path}, Method: {Method}, Query: {Query}, Body: {Body}",
                context.Request.Path,
                context.Request.Method,
                context.Request.QueryString.ToString(),
                await GetRequestBodyAsync(context.Request));

            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Dịch message của <see cref="Error"/> theo Accept-Language rồi trả về kèm mã lỗi cho UI bắt.
    /// </summary>
    private async Task WriteErrorAsync(HttpContext context, Error error)
    {
        var message = error.Args is { Length: > 0 }
            ? _localizer[error.MessageKey, error.Args].Value
            : _localizer[error.MessageKey].Value;

        await WriteExpectedAsync(context, ErrorStatus.ToHttpStatusCode(error.Status), error.Status, message);
    }

    /// <summary>
    /// Trả response lỗi "có thể dự đoán" (4xx): mã lỗi nằm ở Status, message đã bản địa hoá
    /// nằm ở cả Message và Errors[0] để client hiển thị trực tiếp.
    /// </summary>
    private static async Task WriteExpectedAsync(HttpContext context, int statusCode, string status, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(new ApiResponse<object>
        {
            Success = false,
            Status = status,
            Message = message,
            Errors = new List<string> { message },
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>Chuẩn hoá mã lỗi cũ (dạng UPPER_SNAKE) về chữ thường cho client bắt theo mã.</summary>
    private static string ToStatus(string? statusCode)
        => string.IsNullOrWhiteSpace(statusCode) ? ErrorStatus.WrongRequest : statusCode.Trim().ToLowerInvariant();

    private static async Task<string> GetRequestBodyAsync(HttpRequest request)
    {
        if (request.ContentLength == 0 || request.Body == null)
            return "N/A";

        request.EnableBuffering();
        var body = await new StreamReader(request.Body).ReadToEndAsync();
        request.Body.Position = 0;
        return body;
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
	{
		context.Response.ContentType = "application/json";
		context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

		var response = new ApiResponse<object>
		{
			Success = false,
			Status = ErrorStatus.SystemError,
			Message = "An error occurred while processing your request.",
			Errors = new List<string> { exception.Message },
			Timestamp = DateTime.UtcNow
		};

		await context.Response.WriteAsJsonAsync(response);
	}
}
