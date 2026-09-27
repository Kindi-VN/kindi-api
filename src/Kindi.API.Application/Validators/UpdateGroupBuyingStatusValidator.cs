using FluentValidation;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.Resources;
using Kindi.API.Domain.Enums;
using Microsoft.Extensions.Localization;

namespace Kindi.API.Application.Validators;

public class UpdateGroupBuyingStatusValidator : AbstractValidator<UpdateGroupBuyingStatusDto>
{
    public UpdateGroupBuyingStatusValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.Status)
            .Must(status => Enum.IsDefined(typeof(GroupBuyingStatus), status))
            .WithMessage(localizer["GroupBuyingRequest_InvalidStatusMessage"]);

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage(localizer["GroupBuyingRequest_CancelReasonRequired"])
            .When(x => x.Status == (int)GroupBuyingStatus.Cancelled);

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage(localizer["GroupBuyingRequest_ReasonMaxLength"])
            .When(x => !string.IsNullOrEmpty(x.Reason));
    }
}
