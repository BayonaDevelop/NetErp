using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Commons.ExceptionHandlers;

public class ValidationExceptionHandler : IExceptionHandler
{
  public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
  {
    if (exception is ValidationException validationException)
    {
      var errors = validationException.Errors
        .GroupBy(e => e.PropertyName)
        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

      httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

      await httpContext.Response
        .WriteAsJsonAsync(new { errors }, cancellationToken)
        .ConfigureAwait(false);

      return true;
    }

    if (exception is BadHttpRequestException badHttpRequestException)
    {
      httpContext.Response.StatusCode = badHttpRequestException.StatusCode;

      await httpContext.Response
        .WriteAsJsonAsync(new { error = badHttpRequestException.Message }, cancellationToken)
        .ConfigureAwait(false);

      return true;
    }

    return false;
  }
}
