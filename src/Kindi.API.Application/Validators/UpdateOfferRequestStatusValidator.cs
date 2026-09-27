using FluentValidation;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.Resources;
using Microsoft.Extensions.Localization;

namespace Kindi.API.Application.Validators;

public class UpdateOfferRequestStatusValidator : AbstractValidator<UpdateOfferRequestStatusDto>
{
	public UpdateOfferRequestStatusValidator(IStringLocalizer<SharedResource> localizer)
	{
		RuleFor(x => x.Status)
			.IsInEnum()
			.WithMessage(localizer["StatusInvalid"]);
	}
}