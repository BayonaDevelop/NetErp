using FluentValidation.Results;
using Identity.Application.Dto.Requests;
using Identity.Application.Validators;

namespace Identity.Test.Validators;

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
    Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateUserRequestDto.CompanyId));
  }

  [Fact]
  public void Validate_WhenEmailIsMissing_ReturnsError()
  {
    CreateUserRequestDto request = new(1, string.Empty, "SuperSecret1", "Admin");

    ValidationResult result = _sut.Validate(request);

    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateUserRequestDto.Email));
  }

  [Fact]
  public void Validate_WhenEmailFormatIsInvalid_ReturnsError()
  {
    CreateUserRequestDto request = new(1, "not-an-email", "SuperSecret1", "Admin");

    ValidationResult result = _sut.Validate(request);

    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateUserRequestDto.Email));
  }

  [Fact]
  public void Validate_WhenPasswordIsMissing_ReturnsError()
  {
    CreateUserRequestDto request = new(1, "user@test.com", string.Empty, "Admin");

    ValidationResult result = _sut.Validate(request);

    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateUserRequestDto.Password));
  }

  [Fact]
  public void Validate_WhenPasswordIsShorterThanMinimumLength_ReturnsError()
  {
    CreateUserRequestDto request = new(1, "user@test.com", "short1", "Admin");

    ValidationResult result = _sut.Validate(request);

    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateUserRequestDto.Password));
  }

  [Fact]
  public void Validate_WhenRoleIsMissing_ReturnsError()
  {
    CreateUserRequestDto request = new(1, "user@test.com", "SuperSecret1", string.Empty);

    ValidationResult result = _sut.Validate(request);

    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateUserRequestDto.Role));
  }
}
