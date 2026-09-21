using FluentValidation.Results;
using Identity.Application.Dto.Requests;
using Identity.Application.Validators;
using System.Diagnostics.CodeAnalysis;

namespace Identity.Test.Validators;

[SuppressMessage("Style", "IDE0079:Remove unnecessary suppression", Justification = "La supresión CRR0029 es necesaria porque se aplica en Testing")]
[SuppressMessage("Async", "CRR0029:ConfigureAwait unnecessary", Justification = "En el caso de Testing no es necesario especificar el valor de ConfigureAwait.")]
public class CreateUserValidatorTests
{
  private readonly CreateUserValidator _sut = new();

  [Fact]
  public void Validate_WhenRequestIsValid_ReturnsNoErrors()
  {
    CreateUserRequestDto request = new(1, "user@test.com", "SuperSecret1", "Admin");

    ValidationResult result = _sut.Validate(request);

    Assert.True(result.IsValid);
  }

  [Fact]
  public void Validate_WhenCompanyIdIsMissing_ReturnsError()
  {
    CreateUserRequestDto request = new(0, "user@test.com", "SuperSecret1", "Admin");

    ValidationResult result = _sut.Validate(request);

    Assert.False(result.IsValid);
    ValidationFailure error = Assert.Single(result.Errors, e => e.PropertyName.Equals(nameof(CreateUserRequestDto.CompanyId)));
    Assert.Equal("This field is required", error.ErrorMessage);
  }

  [Fact]
  public void Validate_WhenEmailIsMissing_ReturnsError()
  {
    CreateUserRequestDto request = new(1, string.Empty, "SuperSecret1", "Admin");

    ValidationResult result = _sut.Validate(request);

    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, e => e.PropertyName.Equals(nameof(CreateUserRequestDto.Email)));
  }

  [Fact]
  public void Validate_WhenEmailFormatIsInvalid_ReturnsError()
  {
    CreateUserRequestDto request = new(1, "not-an-email", "SuperSecret1", "Admin");

    ValidationResult result = _sut.Validate(request);

    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, e => e.PropertyName.Equals(nameof(CreateUserRequestDto.Email)));
  }

  [Fact]
  public void Validate_WhenPasswordIsMissing_ReturnsError()
  {
    CreateUserRequestDto request = new(1, "user@test.com", string.Empty, "Admin");

    ValidationResult result = _sut.Validate(request);

    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, e => e.PropertyName.Equals(nameof(CreateUserRequestDto.Password)));
  }

  [Fact]
  public void Validate_WhenPasswordIsShorterThanMinimumLength_ReturnsError()
  {
    CreateUserRequestDto request = new(1, "user@test.com", "short1", "Admin");

    ValidationResult result = _sut.Validate(request);

    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, e => e.PropertyName.Equals(nameof(CreateUserRequestDto.Password)));
  }

  [Fact]
  public void Validate_WhenRoleIsMissing_ReturnsError()
  {
    CreateUserRequestDto request = new(1, "user@test.com", "SuperSecret1", string.Empty);

    ValidationResult result = _sut.Validate(request);

    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, e => e.PropertyName.Equals(nameof(CreateUserRequestDto.Role)));
  }
}
