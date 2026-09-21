using FluentValidation.Results;
using Identity.Application.Dto.Requests;
using Identity.Application.Queries;
using Identity.Application.Validators;

namespace Identity.Test.Validators;

public class LoginValidationTests
{
  private readonly LoginValidation _sut = new();

  [Fact]
  public void Validate_WhenRequestIsValid_ReturnsNoErrors()
  {
    LoginQuery query = new(new LoginRequestDto(1, "user@test.com", "secret"), "127.0.0.1");

    ValidationResult result = _sut.Validate(query);

    Assert.True(result.IsValid);
  }

  [Fact]
  public void Validate_WhenCompanyIdIsNotGreaterThanZero_ReturnsError()
  {
    LoginQuery query = new(new LoginRequestDto(0, "user@test.com", "secret"), "127.0.0.1");

    ValidationResult result = _sut.Validate(query);

    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, e => e.PropertyName.EndsWith(nameof(LoginRequestDto.CompanyId)));
  }

  [Fact]
  public void Validate_WhenEmailIsMissing_ReturnsError()
  {
    LoginQuery query = new(new LoginRequestDto(1, string.Empty, "secret"), "127.0.0.1");

    ValidationResult result = _sut.Validate(query);

    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, e => e.PropertyName.EndsWith(nameof(LoginRequestDto.Email)));
  }

  [Fact]
  public void Validate_WhenPasswordIsMissing_ReturnsError()
  {
    LoginQuery query = new(new LoginRequestDto(1, "user@test.com", string.Empty), "127.0.0.1");

    ValidationResult result = _sut.Validate(query);

    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, e => e.PropertyName.EndsWith(nameof(LoginRequestDto.Password)));
  }
}
