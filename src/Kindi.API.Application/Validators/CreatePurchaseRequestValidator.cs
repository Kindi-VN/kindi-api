using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;
using System.Text.RegularExpressions;

namespace Kindi.API.Application.Validators;

/// <summary>
/// Thông tin liên hệ (họ tên/SĐT/email) chỉ bắt buộc với khách chưa đăng nhập —
/// người đã đăng nhập được bù từ hồ sơ tài khoản ở tầng service.
/// </summary>
public class CreatePurchaseRequestValidator : AbstractValidator<CreatePurchaseRequestDto>
{
    public CreatePurchaseRequestValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage(localizer["PurchaseRequest_ProductNameRequired"])
            .MinimumLength(3).WithMessage(localizer["PurchaseRequest_ProductNameMinLength"]);

        RuleFor(x => x.ProductCategory)
            .MaximumLength(100).WithMessage(localizer["PurchaseRequest_ProductCategoryLength"])
            .When(x => !string.IsNullOrEmpty(x.ProductCategory));

        RuleFor(x => x.Quantity)
            .NotEmpty().WithMessage(localizer["PurchaseRequest_QuantityRequired"])
            .GreaterThan(0).WithMessage(localizer["PurchaseRequest_QuantityPositive"]);

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage(localizer["PurchaseRequest_UnitRequired"])
            .MaximumLength(50).WithMessage(localizer["PurchaseRequest_UnitLength"]);

        RuleFor(x => x.FullName)
            .MinimumLength(2).WithMessage(localizer["PurchaseRequest_FullNameMinLength"])
            .When(x => !string.IsNullOrEmpty(x.FullName));

        RuleFor(x => x.Phone)
            .Must(phone => Regex.IsMatch(phone!, @"^0[0-9]{9,10}$"))
            .WithMessage(localizer["PurchaseRequest_PhoneInvalid"])
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Zalo)
            .Must(zalo => Regex.IsMatch(zalo!, @"^0[0-9]{9,10}$"))
            .WithMessage(localizer["PurchaseRequest_ZaloInvalid"])
            .When(x => !string.IsNullOrEmpty(x.Zalo));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage(localizer["PurchaseRequest_EmailInvalid"])
            .When(x => !string.IsNullOrEmpty(x.Email));
    }
}