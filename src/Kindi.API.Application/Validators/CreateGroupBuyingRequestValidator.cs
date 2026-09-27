// src/Kindi.API.Application/Validators/CreateGroupBuyingRequestValidator.cs
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
public class CreateGroupBuyingRequestValidator : AbstractValidator<CreateGroupBuyingRequestDto>
{
    public CreateGroupBuyingRequestValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage(localizer["GroupBuyingRequest_ProductNameRequired"])
            .MinimumLength(3).WithMessage(localizer["GroupBuyingRequest_ProductNameMinLength"]);

        RuleFor(x => x.ProductLink)
            .Must(link => string.IsNullOrEmpty(link) || Uri.IsWellFormedUriString(link, UriKind.Absolute))
            .WithMessage(localizer["GroupBuyingRequest_ProductLinkInvalid"]);

        RuleFor(x => x.TargetPrice)
            .NotEmpty().WithMessage(localizer["GroupBuyingRequest_TargetPriceRequired"])
            .GreaterThanOrEqualTo(1000).WithMessage(localizer["GroupBuyingRequest_TargetPriceMin"]);

        RuleFor(x => x.TargetPeopleCount)
            .NotEmpty().WithMessage(localizer["GroupBuyingRequest_TargetPeopleCountRequired"])
            .GreaterThanOrEqualTo(2).WithMessage(localizer["GroupBuyingRequest_TargetPeopleCountMin"]);

        RuleFor(x => x.FullName)
            .MinimumLength(2).WithMessage(localizer["GroupBuyingRequest_FullNameMinLength"])
            .MaximumLength(100).WithMessage(localizer["GroupBuyingRequest_FullNameMaxLength"])
            .When(x => !string.IsNullOrEmpty(x.FullName));

        RuleFor(x => x.Phone)
            .Must(phone => Regex.IsMatch(phone!, @"^0[0-9]{9,10}$"))
            .WithMessage(localizer["GroupBuyingRequest_PhoneInvalid"])
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Zalo)
            .Must(zalo => Regex.IsMatch(zalo!, @"^[0-9]{10}$"))
            .WithMessage(localizer["GroupBuyingRequest_ZaloInvalid"])
            .When(x => !string.IsNullOrEmpty(x.Zalo));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage(localizer["GroupBuyingRequest_EmailInvalid"])
            .MaximumLength(100).WithMessage(localizer["GroupBuyingRequest_EmailMaxLength"])
            .When(x => !string.IsNullOrEmpty(x.Email));
    }
}