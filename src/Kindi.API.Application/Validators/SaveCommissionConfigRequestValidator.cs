using FluentValidation;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.Resources;
using Kindi.API.Domain.Enums;
using Microsoft.Extensions.Localization;

/// <summary>Kiểm tra yêu cầu lưu cấu hình hoa hồng trước khi ghi.</summary>
public class SaveCommissionConfigRequestValidator : AbstractValidator<SaveCommissionConfigRequest>
{
    public SaveCommissionConfigRequestValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.Beneficiary)
            .IsInEnum().WithMessage(localizer["Commission_InvalidBeneficiary"]);

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage(localizer["Commission_InvalidType"]);

        RuleFor(x => x.Rate)
            .GreaterThan(0).WithMessage(localizer["Commission_RateRequired"]);

        RuleFor(x => x.Rate)
            .LessThanOrEqualTo(100).WithMessage(localizer["Commission_RatePercentMax"])
            .When(x => x.Type == CommissionType.Percentage);

        RuleFor(x => x.UserIds)
            .NotEmpty().WithMessage(localizer["Commission_NoTarget"])
            .When(x => !x.IsGlobal);

        RuleFor(x => x.MinOrderValue)
            .GreaterThanOrEqualTo(0).WithMessage(localizer["Commission_MinOrderValueInvalid"])
            .When(x => x.MinOrderValue.HasValue);

        RuleFor(x => x.MaxCommission)
            .GreaterThan(0).WithMessage(localizer["Commission_MaxCommissionInvalid"])
            .When(x => x.MaxCommission.HasValue);

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage(localizer["Commission_NoteTooLong"]);

        RuleFor(x => x.Tiers)
            .NotEmpty().WithMessage(localizer["Commission_TiersRequired"])
            .When(x => x.Type == CommissionType.Tiered);

        RuleForEach(x => x.Tiers).ChildRules(tier =>
        {
            tier.RuleFor(t => t.Rate)
                .GreaterThanOrEqualTo(0).WithMessage(localizer["Commission_TierRateInvalid"]);

            tier.RuleFor(t => t.ToValue)
                .GreaterThan(t => t.FromValue).WithMessage(localizer["Commission_TierRangeInvalid"])
                .When(t => t.ToValue.HasValue);
        });

        RuleFor(x => x.Tiers)
            .Must(tiers => !tiers.Where(t => t.ToValue.HasValue).GroupBy(t => t.ToValue!.Value).Any(g => g.Count() > 1))
            .WithMessage(localizer["Commission_TierOverlap"])
            .When(x => x.Type == CommissionType.Tiered && x.Tiers.Count > 1);
    }
}
