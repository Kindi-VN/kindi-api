using FluentValidation;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.Resources;
using Microsoft.Extensions.Localization;
using System.Text.RegularExpressions;

namespace Kindi.API.Application.Validators;

/// <summary>
/// Admin sửa yêu cầu mua chung — chỉ validate field nào được gửi lên (partial update),
/// dùng cùng ngưỡng với CreateGroupBuyingRequestValidator.
/// </summary>
public class UpdateGroupBuyingRequestValidator : AbstractValidator<UpdateGroupBuyingRequestDto>
{
    public UpdateGroupBuyingRequestValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.ProductName)
            .MinimumLength(3).WithMessage(localizer["GroupBuyingRequest_ProductNameMinLength"])
            .MaximumLength(255).WithMessage(localizer["GroupBuyingRequest_ProductNameMaxLength"])
            .When(x => !string.IsNullOrWhiteSpace(x.ProductName));

        RuleFor(x => x.ProductLink)
            .Must(link => Uri.IsWellFormedUriString(link, UriKind.Absolute))
            .WithMessage(localizer["GroupBuyingRequest_ProductLinkInvalid"])
            .When(x => !string.IsNullOrWhiteSpace(x.ProductLink));

        RuleFor(x => x.TargetPrice)
            .GreaterThanOrEqualTo(1000).WithMessage(localizer["GroupBuyingRequest_TargetPriceMin"])
            .When(x => x.TargetPrice.HasValue);

        RuleFor(x => x.TargetPeopleCount)
            .GreaterThanOrEqualTo(2).WithMessage(localizer["GroupBuyingRequest_TargetPeopleCountMin"])
            .When(x => x.TargetPeopleCount.HasValue);

        RuleFor(x => x.Note)
            .MaximumLength(1000).WithMessage(localizer["GroupBuyingRequest_NoteMaxLength"])
            .When(x => !string.IsNullOrEmpty(x.Note));
    }
}
