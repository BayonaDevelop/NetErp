using Commons.EndPoints;
using Commons.I18n;
using Commons.Mediator;
using Identity.Application.Commands;
using Identity.Application.Dto.Requests;
using Identity.Application.Dto.Responses;
using Identity.Application.Queries;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Api.Endpoints;

public class AuthenticationEndpoits : IEndpoint
{
  public static void MapEndpoints(IEndpointRouteBuilder app)
  {
      
    ResxLocalizer _generic = new("Commons.Resources.GenericMessages", typeof(ResxLocalizer).Assembly);
    ResxLocalizer _messages = new("Identity.Application.Resources.UserMessages", typeof(LoginQuery).Assembly);

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

        return dispatcher
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
        
        string? ip = http.Connection.RemoteIpAddress?.ToString();
        LoginResponseDto response = await dispatcher
          .SendAsync(new LoginQuery(request, ip ?? string.Empty), cancellationToken)
          .ConfigureAwait(false);

        if (response.AccessToken.Equals(string.Empty))
        {
          if (logger.IsEnabled(LogLevel.Error))
            logger.LogError("{Message}", _messages.Get("LOGIN_FAILED"));
          return Results.Unauthorized();
        }
        else
        {
          if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("{Message}", _messages.Get("LOGIN_SUCCESS"));
          return Results.Ok(response);
        }
      }
    );

    group.MapPost(
      "refresh-token",
      async (
        RefreshTokenRequestDto request,
        IDispatcher dispatcher,
        HttpContext http,
        ILogger<AuthenticationEndpoits> logger,
        CancellationToken cancellationToken
      ) =>
      {
        string? ip = http.Connection.RemoteIpAddress?.ToString();
        LoginResponseDto response = await dispatcher
          .SendAsync(new RefreshTokenCommand(request, ip ?? string.Empty), cancellationToken)
          .ConfigureAwait(false);


        if (response.AccessToken.Equals(string.Empty))
        {
          if (logger.IsEnabled(LogLevel.Error))
            logger.LogError("{Message}", _messages.Get("LOGIN_FAILED"));
          return Results.Unauthorized();
        }
        else
        {
          if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("{Message}", _messages.Get("LOGIN_SUCCESS"));
          return Results.Ok(response);
        }
      }
    );

    group.MapPost(
      "logout",
      async (
        RefreshTokenRequestDto request,
        IDispatcher dispatcher,
        ILogger<AuthenticationEndpoits> logger,
        CancellationToken cancellationToken
      ) =>
      {
        await dispatcher
          .SendAsync(new LogoutCommand(request), cancellationToken)
          .ConfigureAwait(false);

        return Results.NoContent();
      }
    );

    group.MapGet(
      "get-company-users",
      async (
        long? companyId,
        IDispatcher dispatcher,
        ILogger<AuthenticationEndpoits> logger,
        CancellationToken cancellationToken
      ) =>
      {
        if (!companyId.HasValue)
          return Results.BadRequest(new { message = _generic.Get("FIELD_REQUIRED") });

        if (companyId <= 0)
          return Results.BadRequest(new { message = _messages.Get("INCORRECT_NUMBER_VALUE") });

        List<UserResponseDto> response = await dispatcher
          .SendAsync(new GetUsersByCompanyQuery(companyId!.Value), cancellationToken)
          .ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
        {
          if (response.Count == 0)
            logger.LogInformation("{Message}", _generic.Get("RECORDS_NOT_FOUND"));
          else
            logger.LogWarning("{Message}", _generic.Get("RECORDS_FOUND", response.Count));
        }

        return Results.Ok(response);
      }
    );

    group.MapGet(
      "get-user",
      async (
        long? companyId,
        long? userId,
        IDispatcher dispatcher,
        ILogger<AuthenticationEndpoits> logger,
        CancellationToken cancellationToken
      ) =>
      {
        if (!companyId.HasValue)
          return Results.BadRequest(new { message = _generic.Get("FIELD_REQUIRED") });

        if (companyId <= 0)
          return Results.BadRequest(new { message = _messages.Get("INCORRECT_NUMBER_VALUE") });

        if (!userId.HasValue)
          return Results.BadRequest(new { message = _generic.Get("FIELD_REQUIRED") });

        if (userId <= 0)
          return Results.BadRequest(new { message = _messages.Get("INCORRECT_NUMBER_VALUE") });

        UserResponseDto response = await dispatcher
          .SendAsync(new GetUserByIdQuery(companyId!.Value, userId!.Value), cancellationToken)
          .ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information) && response.Email.IsNullOrEmpty())
        {
          logger.LogInformation("{Message}", _messages.Get("USER_NOT_FOUND"));
        }

        return Results.Ok(response);
      }
    );
  }
}
