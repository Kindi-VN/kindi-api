using Kindi.API.Shared.Errors;

namespace Kindi.API.Application.Errors;

/// <summary>Lỗi nghiệp vụ của yêu cầu báo giá.</summary>
public static class OfferRequestError
{
    public static Error FullNameRequired => new(ErrorStatus.WrongRequest, "OfferRequest_FullNameRequired");

    public static Error PhoneRequired => new(ErrorStatus.WrongRequest, "OfferRequest_PhoneRequired");

    public static Error NotFound => new(ErrorStatus.NotFound, "OfferRequestNotFound");
}
