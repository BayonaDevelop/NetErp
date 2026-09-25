using FluentValidation.Results;
using Identity.Application.Commands;
using Identity.Application.Dto.Requests;
using Identity.Application.Validators;
using System.Diagnostics.CodeAnalysis;

namespace Identity.Test.Validators;

[SuppressMessage("Style", "IDE0079:Remove unnecessary suppression", Justification = "La supresión CRR0029 es necesaria porque se aplica en Testing")]
[SuppressMessage("Async", "CRR0029:ConfigureAwait unnecessary", Justification = "En el caso de Testing no es necesario especificar el valor de ConfigureAwait.")]
public class RefreshTokenValidatorTests
{
  private readonly RefreshTokenValidator _sut = new();

  [Fact]
  public void Validate_WhenRequestIsValid_ReturnsNoErrors()
  {
    RefreshTokenCommand command = new(new RefreshTokenRequestDto("some-token"), "127.0.0.1");

    ValidationResult result = _sut.Validate(command);

    Assert.True(result.IsValid);
  }

  [Fact]
  public void Validate_WhenRefreshTokenIsMissing_ReturnsError()
  {
    RefreshTokenCommand command = new(new RefreshTokenRequestDto(string.Empty), "127.0.0.1");

    ValidationResult result = _sut.Validate(command);

    Assert.False(result.IsValid);
    ValidationFailure error = Assert.Single(result.Errors, e => e.PropertyName.EndsWith(nameof(RefreshTokenRequestDto.RefreshToken)));
    Assert.Equal("This field is required", error.ErrorMessage);
  }
}
