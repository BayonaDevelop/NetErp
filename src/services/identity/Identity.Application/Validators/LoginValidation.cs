using FluentValidation;
using Identity.Application.Query;

namespace Identity.Application.Validators;

public class LoginValidation : AbstractValidator<LoginQuery>
{
  public LoginValidation()
  {
      RuleFor(i => i.Email)
      .NotEmpty()
      .WithMessage("Email is required.");

      RuleFor(i => i.Password)
        .NotEmpty()
        .WithMessage("Password is required.");
  }
}
