using System.Text.RegularExpressions;
using ClientDiscord.Models;
using ClientDiscord.Primitivies;
using FluentValidation;

namespace ClientDiscord.Validators;

public class GuildUpdateValidator : AbstractValidator<UpdateGuildRequest>
{
    private readonly List<string> _allowedRegions;
    public GuildUpdateValidator(List<string> allowedRegions)
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
            .Matches(@"^[a-zA-Zа-яА-Я]+$").WithMessage(x => ValidationMessages.InvalidProperty(nameof(x.Description)))
            .NotEmpty().WithMessage(x => ValidationMessages.NotEmpty(nameof(x.Description)))
            .NotNull().WithMessage(x => ValidationMessages.NotNull(nameof(x.Description)));
    }
}
