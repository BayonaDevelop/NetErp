using Commons.I18n;
using FluentValidation;
using Identity.Application.Commands;

namespace Identity.Application.Validators;

public class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
  public RefreshTokenValidator()
  {
    ResxLocalizer generic = new("Commons.Resources.GenericMessages", typeof(ResxLocalizer).Assembly);

    RuleFor(i => i.Request.RefreshToken)
        .NotEmpty()
        .WithMessage(generic.Get("FIELD_REQUIRED"));
  }
}
