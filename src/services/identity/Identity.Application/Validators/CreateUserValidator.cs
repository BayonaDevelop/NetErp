using Commons.I18n;
using FluentValidation;
using Identity.Application.Dto.Requests;

namespace Identity.Application.Validators;

public class CreateUserValidator : AbstractValidator<CreateUserRequestDto>
{
  public CreateUserValidator()
  {
    ResxLocalizer generic = new("Commons.Resources.GenericMessages", typeof(ResxLocalizer).Assembly);

    RuleFor(i => i.CompanyId)
      .NotEmpty()
      .WithMessage(generic.Get("FIELD_REQUIRED"));

    RuleFor(i => i.Email)
      .NotEmpty()
      .WithMessage(generic.Get("FIELD_REQUIRED"));

    RuleFor(i => i.Email)
      .EmailAddress()
      .WithMessage(generic.Get("MAIL_FORMAT"));

    RuleFor(i => i.Password)
      .NotEmpty()
      .WithMessage(generic.Get("FIELD_REQUIRED"));

    RuleFor(i => i.Password)
      .MinimumLength(10)
      .WithMessage(generic.Get("PASSWORD_LENGTH", 10));

    RuleFor(i => i.Role)
      .NotEmpty()
      .WithMessage(generic.Get("FIELD_REQUIRED"));
  }
}
