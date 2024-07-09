using ClientDiscord.Models;
using ClientDiscord.Primitivies;
using Discord;
using FluentValidation;

namespace ClientDiscord.Validators;

public class ChannelUpdateValidator : AbstractValidator<UpdateChannelRequest>
{
    private readonly List<string> _allowedRegions;
    public ChannelUpdateValidator(List<string> allowedRegions)
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
        RuleFor(x => x.Topic)
            .NotNull().WithMessage(x => ValidationMessages.NotNull(nameof(x.Topic)))
            .NotEmpty().WithMessage(x => ValidationMessages.NotEmpty(nameof(x.Topic)));
        RuleFor(x => x.UserLimit)
            .GreaterThan(0).WithMessage(x => ValidationMessages.InvalidProperty(nameof(x.UserLimit)))
            .NotNull().WithMessage(x => ValidationMessages.NotNull(nameof(x.UserLimit)))
            .NotEmpty().WithMessage(x => ValidationMessages.NotEmpty(nameof(x.UserLimit)));
    }
}