using Commons.I18n;
using FluentValidation;
using Identity.Application.Queries;

namespace Identity.Application.Validators;

public class LoginValidation : AbstractValidator<LoginQuery>
{
  public LoginValidation()
  {
    ResxLocalizer generic = new("Commons.Resources.GenericMessages", typeof(ResxLocalizer).Assembly);

    RuleFor(i => i.Request.CompanyId)
      .GreaterThan(0)
      .WithMessage(generic.Get("FIELD_REQUIRED"));

    RuleFor(i => i.Request.Email)
      .NotEmpty()
      .WithMessage(generic.Get("FIELD_REQUIRED"));

      RuleFor(i => i.Request.Password)
        .NotEmpty()
        .WithMessage(generic.Get("FIELD_REQUIRED"));
  }
}
