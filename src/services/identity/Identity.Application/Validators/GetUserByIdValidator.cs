using Commons.I18n;
using FluentValidation;
using Identity.Application.Dto.Requests;
using Identity.Application.Queries;

namespace Identity.Application.Validators;

public class GetUserByIdValidator : AbstractValidator<GetUserByIdRequestDto>
{
  public GetUserByIdValidator()
  {
    ResxLocalizer _generic = new("Commons.Resources.GenericMessages", typeof(ResxLocalizer).Assembly);
    ResxLocalizer _messages = new("Identity.Application.Resources.UserMessages", typeof(LoginQuery).Assembly);

    RuleFor(i => i.CompanyId)
      .NotNull()
      .WithMessage(_generic.Get("FIELD_REQUIRED"));

    RuleFor(i => i.CompanyId)
      .GreaterThan(0)
      .WithMessage(_messages.Get("INCORRECT_NUMBER_VALUE"));

    RuleFor(i => i.UserId)
      .NotNull()
      .WithMessage(_generic.Get("FIELD_REQUIRED"));

    RuleFor(i => i.UserId)
      .GreaterThan(0)
      .WithMessage(_messages.Get("INCORRECT_NUMBER_VALUE"));


  }
}
