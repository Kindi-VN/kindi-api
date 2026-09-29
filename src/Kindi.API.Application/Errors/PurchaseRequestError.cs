using Kindi.API.Shared.Errors;

namespace Kindi.API.Application.Errors;

/// <summary>Lỗi nghiệp vụ của yêu cầu mua.</summary>
public static class PurchaseRequestError
{
    public static Error FullNameRequired => new(ErrorStatus.WrongRequest, "PurchaseRequest_FullNameRequired");

    public static Error PhoneRequired => new(ErrorStatus.WrongRequest, "PurchaseRequest_PhoneRequired");

    public static Error EmailRequired => new(ErrorStatus.WrongRequest, "PurchaseRequest_EmailRequired");

    public static Error NotFound => new(ErrorStatus.NotFound, "PurchaseRequestNotFound");
}
