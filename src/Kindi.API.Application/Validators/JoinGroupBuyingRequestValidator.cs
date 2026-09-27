using FluentValidation;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.Resources;
using Microsoft.Extensions.Localization;
using System.Text.RegularExpressions;

namespace Kindi.API.Application.Validators;

/// <summary>
/// Khách chưa đăng nhập phải gửi họ tên + số điện thoại (dùng để tạo tài khoản và liên hệ).
/// Người đã đăng nhập chỉ gửi ghi chú nên các field kia để trống vẫn hợp lệ.
/// </summary>
public class JoinGroupBuyingRequestValidator : AbstractValidator<JoinGroupBuyingRequestDto>
{
    public JoinGroupBuyingRequestValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.FullName)
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

        RuleFor(x => x.Note)
            .MaximumLength(1000).WithMessage(localizer["GroupBuyingRequest_NoteMaxLength"])
            .When(x => !string.IsNullOrEmpty(x.Note));
    }
}
