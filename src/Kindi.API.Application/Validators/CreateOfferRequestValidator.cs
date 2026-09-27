// src/Kindi.API.Application/Validators/CreateOfferRequestValidator.cs
using Kindi.API.Application.DTOs.requests;
using FluentValidation;
using Microsoft.Extensions.Localization;
using Kindi.API.Application.Resources;

namespace Kindi.API.Application.Validators;

/// <summary>
/// Thông tin liên hệ (họ tên/SĐT) chỉ bắt buộc với khách chưa đăng nhập —
/// người đã đăng nhập được bù từ hồ sơ tài khoản ở tầng handler.
/// </summary>
public class CreateOfferRequestValidator : AbstractValidator<CreateOfferRequestDto>
{
    public CreateOfferRequestValidator(IStringLocalizer<SharedResource> localizer)
    {
        // Product information
        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage(localizer["OfferRequest_ProductNameRequired"])
            .MinimumLength(3).WithMessage(localizer["OfferRequest_ProductNameMinLength"]);

        RuleFor(x => x.ProductLink)
            .Must(link => string.IsNullOrEmpty(link) || Uri.IsWellFormedUriString(link, UriKind.Absolute))
            .WithMessage(localizer["OfferRequest_ProductLinkInvalid"])
            .When(x => !string.IsNullOrEmpty(x.ProductLink));

        RuleFor(x => x.CurrentPrice)
            .NotEmpty().WithMessage(localizer["OfferRequest_CurrentPriceRequired"])
            .GreaterThan(0).WithMessage(localizer["OfferRequest_CurrentPriceInvalid"]);

        RuleFor(x => x.ExpectedPrice)
            .GreaterThan(0).WithMessage(localizer["OfferRequest_ExpectedPriceInvalid"])
            .When(x => x.ExpectedPrice.HasValue);

        RuleFor(x => x.Quantity)
            .NotEmpty().WithMessage(localizer["OfferRequest_QuantityRequired"])
            .GreaterThanOrEqualTo(1).WithMessage(localizer["OfferRequest_QuantityMin"]);

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage(localizer["OfferRequest_UnitRequired"])
            .MinimumLength(1).WithMessage(localizer["OfferRequest_UnitMinLength"]);

        // User information
        RuleFor(x => x.FullName)
            .MinimumLength(2).WithMessage(localizer["OfferRequest_FullNameMinLength"])
            .When(x => !string.IsNullOrEmpty(x.FullName));

        RuleFor(x => x.Phone)
            .Must(phone => System.Text.RegularExpressions.Regex.IsMatch(phone!, @"^0[0-9]{9,10}$"))
            .WithMessage(localizer["OfferRequest_PhoneInvalid"])
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage(localizer["OfferRequest_EmailInvalid"])
            .When(x => !string.IsNullOrEmpty(x.Email));
    }
}