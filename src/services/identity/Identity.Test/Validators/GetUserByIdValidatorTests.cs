using FluentValidation.Results;
using Identity.Application.Dto.Requests;
using Identity.Application.Validators;

namespace Identity.Test.Validators;

public class GetUserByIdValidatorTests
{
  private readonly GetUserByIdValidator _sut = new();

  [Fact]
  public void Validate_WhenRequestIsValid_ReturnsNoErrors()
  {
    GetUserByIdRequestDto request = new(1, 1);

    ValidationResult result = _sut.Validate(request);

    Assert.True(result.IsValid);
  }

  [Fact]
  public void Validate_WhenCompanyIdIsNotPositive_ReturnsError()
  {
    GetUserByIdRequestDto request = new(0, 1);

    ValidationResult result = _sut.Validate(request);

    Assert.False(result.IsValid);
    ValidationFailure error = Assert.Single(result.Errors, e => e.PropertyName.Equals(nameof(GetUserByIdRequestDto.CompanyId)));
    Assert.Equal("This field only accepts numbers greater than 0.", error.ErrorMessage);
  }

  [Fact]
  public void Validate_WhenUserIdIsNotPositive_ReturnsError()
  {
    GetUserByIdRequestDto request = new(1, 0);

    ValidationResult result = _sut.Validate(request);

    Assert.False(result.IsValid);
    ValidationFailure error = Assert.Single(result.Errors, e => e.PropertyName.Equals(nameof(GetUserByIdRequestDto.UserId)));
    Assert.Equal("This field only accepts numbers greater than 0.", error.ErrorMessage);
  }
}
