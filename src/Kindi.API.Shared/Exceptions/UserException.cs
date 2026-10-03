using Kindi.API.Shared.Exceptions;
using Kindi.API.Shared.Resources;
using Microsoft.Extensions.Localization;

namespace Kindi.API.Shared.Exceptions;

public static class UserException
{
    public static KindiException PhoneRequired(IStringLocalizer<ExceptionMessages> localizer)
        => new KindiException(
            statusCode: "USER_PHONE_REQUIRED",
            message: localizer["User_PhoneRequired"]
        );

    public static KindiException PhoneAlreadyExists(IStringLocalizer<ExceptionMessages> localizer, string phone)
        => new KindiException(
            statusCode: "USER_PHONE_EXISTS",
            message: localizer["User_PhoneAlreadyExists", phone],
            additionalData: new { Phone = phone }
        );

    public static KindiException AdminResetNotAllowed(IStringLocalizer<ExceptionMessages> localizer)
        => new KindiException(
            statusCode: "USER_ADMIN_RESET_NOT_ALLOWED",
            message: localizer["User_AdminResetNotAllowed"]
        );

    public static KindiException EmailAlreadyExists(IStringLocalizer<ExceptionMessages> localizer, string email)
        => new KindiException(
            statusCode: "USER_EMAIL_EXISTS",
            message: localizer["User_EmailAlreadyExists", email],
            additionalData: new { Email = email }
        );
}