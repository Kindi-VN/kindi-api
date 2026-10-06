namespace Kindi.API.WebApi.Middlewares;

using System.Globalization;
using Kindi.API.Shared.Common.Helpers;
using Kindi.API.Shared.Constants;

/// <summary>
/// Trả nội dung dịch theo ngôn ngữ người dùng chọn lúc đăng nhập: đọc claim ngôn ngữ trong token rồi đặt
/// culture cho request (thay cho việc client phải gửi header Accept-Language ở từng lời gọi).
/// Khách chưa đăng nhập không có claim → giữ nguyên ngôn ngữ mà <c>UseRequestLocalization</c> đã suy ra.
/// Phải đăng ký SAU <c>UseAuthentication</c> thì mới có <see cref="HttpContext.User"/>.
/// </summary>
public class TokenCultureMiddleware
{
    private readonly RequestDelegate _next;

    public TokenCultureMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var language = LanguageHelper.Normalize(
            context.User?.FindFirst(AuthClaimConstants.Language)?.Value);

        if (language != null)
        {
            var culture = new CultureInfo(language);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        await _next(context);
    }
}
