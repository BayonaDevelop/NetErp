using Commons.EndPoints;
using Commons.I18n;
using Commons.Mediator;
using Identity.Application.Commands;
using Identity.Application.Dto.Requests;
using Identity.Application.Dto.Responses;
using Identity.Application.Handlers;
using Identity.Application.Queries;

namespace Identity.Api.Endpoints;

public class AuthenticationEndpoits : IEndpoint
{
  private static readonly ResxLocalizer _messages = new("Identity.Api.Resources.GenericMessages", typeof(CreateUserHandler).Assembly);

  public static void MapEndpoints(IEndpointRouteBuilder app)
  {
    RouteGroupBuilder group = app
      .MapGroup("api/v1/auth/")
      .WithTags("Autenticacion");

    group.MapPost(
      "create-user",
      async (
        CreateUserRequestDto request,
        IDispatcher dispatcher,
        HttpContext http,
        ILogger<AuthenticationEndpoits> logger,
        CancellationToken cancellationToken
      ) =>
      {
        string? ip = http.Connection.RemoteIpAddress?.ToString();

        return await dispatcher
          .SendAsync(new CreateUserCommand(request, ip!), cancellationToken)
          .ConfigureAwait(false);

      }
    );

    group.MapPost(
      "login",
      async (
        LoginRequestDto request,
        IDispatcher dispatcher,
        HttpContext http,
        ILogger<AuthenticationEndpoits> logger,
        CancellationToken cancellationToken
      ) =>
      {
        ResxLocalizer _messages = new("Identity.Application.Resources.Messages", typeof(LoginQuery).Assembly);
        string? ip = http.Connection.RemoteIpAddress?.ToString();
        LoginResponseDto response = await dispatcher
          .SendAsync(new LoginQuery(request, ip ?? string.Empty), cancellationToken)
          .ConfigureAwait(false);

        if (response.AccessToken.Equals(string.Empty))
        {
          if (logger.IsEnabled(LogLevel.Error))
          {
            logger.LogError("{Message}", _messages.Get("LoginFailed"));
          }
          return Results.Unauthorized();
        }
        else
        {
          if (logger.IsEnabled(LogLevel.Information))
          {
            logger.LogInformation("{Message}", _messages.Get("LoginSuccessful", request.Email));
          }
          return Results.Ok(response);
        }
      }
    );

    group.MapGet(
      "get-company-users/{companyId}",
      async (
        long companyId,
        IDispatcher dispatcher,
        ILogger<AuthenticationEndpoits> logger,
        CancellationToken cancellationToken
      ) =>
      {
        List<UserResponseDto> response = await dispatcher
          .SendAsync(new GetUsersByCompanyQuery(companyId), cancellationToken)
          .ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
        {
          if (response.Count == 0)
            logger.LogInformation("{Message}", _messages.Get("RECORDS_NOT_FOUND"));
          else
            logger.LogWarning("{Message}", _messages.Get("RECORDS_FOUND", response.Count));
        }

        return Results.Ok(response);
      }
    );
  }
}
