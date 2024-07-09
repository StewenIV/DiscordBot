using ClientDiscord.Models;
using ClientDiscord.Primitivies;
using FluentValidation;

namespace ClientDiscord.Validators;

public class GuildCreateValidator: AbstractValidator<CreateGuildRequest>
{
    private readonly List<string> _allowedRegions;
    public GuildCreateValidator(List<string> allowedRegions)
    {
        _allowedRegions = allowedRegions;
        RuleFor(x => x.Name)
            .NotNull().WithMessage(x => ValidationMessages.NotNull(nameof(x.Name)))
            .NotEmpty().WithMessage(x => ValidationMessages.NotEmpty(nameof(x.Name)));
        RuleFor(x => x.Region)
            .Must(region => _allowedRegions.Contains(region.ToLower())).WithMessage(x =>
            {
                var listAllowedRegions = string.Join(", \n", _allowedRegions);
                return ValidationMessages.InvalidProperty(nameof(x.Region)+$"(\nAllowed regions: {listAllowedRegions})");
            })
            .NotNull().WithMessage(x => ValidationMessages.NotNull(nameof(x.Region)))
            .NotEmpty().WithMessage(x => ValidationMessages.NotEmpty(nameof(x.Region)));
        RuleFor(x => x.AfkTimeout)
            .GreaterThan(0).WithMessage(x => ValidationMessages.InvalidProperty(nameof(x.AfkTimeout)))
            .NotNull().WithMessage(x => ValidationMessages.NotNull(nameof(x.AfkTimeout)))
            .NotEmpty().WithMessage(x => ValidationMessages.NotEmpty(nameof(x.AfkTimeout)));
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage(x => ValidationMessages.NotEmpty(nameof(x.Description)))
            .NotNull().WithMessage(x => ValidationMessages.NotNull(nameof(x.Description)));
    }
}