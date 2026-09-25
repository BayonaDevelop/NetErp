using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace Gateway.Security;

public static class JwtAuthenticationExtensions
{
  public const string SwaggerPolicy = "Swagger";

  // Debe coincidir con el NormalizedName del rol en Identity, no con su Name.
  public const string SwaggerRole = "SWAGGER";

  public const string SwaggerCookieName = "netErp_swagger_token";

  private const string LoginPath = "/login";

  public static WebApplicationBuilder AddJwtAuthentication(this WebApplicationBuilder builder)
  {
    JwtSettings settings = builder.Configuration
      .GetSection(JwtSettings.SectionName)
      .Get<JwtSettings>()
      ?? throw new InvalidOperationException($"Missing '{JwtSettings.SectionName}' configuration section.");

    builder.Services
      .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
      .AddJwtBearer(options =>
      {
        options.TokenValidationParameters = new TokenValidationParameters
        {
          ValidateIssuer = true,
          ValidIssuer = settings.Issuer,
          ValidateAudience = true,
          ValidAudience = settings.Audience,
          ValidateIssuerSigningKey = true,
          IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey!)),
          ValidateLifetime = true,
          ClockSkew = TimeSpan.FromSeconds(30)
        };

        options.Events = new JwtBearerEvents
        {
          // Permite navegar a /swagger o /scalar con la cookie que /login
          // establece, en vez de exigir siempre un header Authorization.
          OnMessageReceived = context =>
          {
            if (string.IsNullOrEmpty(context.Token) &&
                context.Request.Cookies.TryGetValue(SwaggerCookieName, out string? cookieToken))
            {
              context.Token = cookieToken;
            }
            return Task.CompletedTask;
          },

          // Un navegador sin sesion valida se manda al formulario de login
          // en vez de recibir un 401 crudo; un cliente API conserva el 401.
          OnChallenge = context =>
          {
            if (!IsBrowserNavigation(context.Request))
              return Task.CompletedTask;

            context.HandleResponse();
            string returnUrl = context.Request.Path + context.Request.QueryString;
            context.Response.Redirect($"{LoginPath}?error=1&returnUrl={Uri.EscapeDataString(returnUrl)}");
            return Task.CompletedTask;
          },

          OnForbidden = context =>
          {
            if (!IsBrowserNavigation(context.Request))
              return Task.CompletedTask;

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "text/plain; charset=utf-8";
            return context.Response.WriteAsync($"Acceso denegado: se requiere el rol '{SwaggerRole}'.");
          }
        };
      });

    builder.Services
      .AddAuthorizationBuilder()
      .AddPolicy(SwaggerPolicy, policy => policy.RequireClaim(ClaimTypes.Role, SwaggerRole));

    return builder;
  }

  private static bool IsBrowserNavigation(HttpRequest request) =>
    request.Headers.Accept.Any(a => a is not null && a.Contains("text/html", StringComparison.OrdinalIgnoreCase));

  /// <summary>
  /// Aplica una policy de autorizacion a rutas servidas por middleware que no
  /// pasa por endpoint routing (p.ej. UseSwaggerUI), donde RequireAuthorization()
  /// no esta disponible.
  /// </summary>
  public static WebApplication RequireAuthorizationForPath(this WebApplication app, PathString pathPrefix, string policyName)
  {
    app.Use(async (context, next) =>
    {
      if (!context.Request.Path.StartsWithSegments(pathPrefix))
      {
        await next(context).ConfigureAwait(false);
        return;
      }

      IAuthorizationService authorizationService = context.RequestServices.GetRequiredService<IAuthorizationService>();
      AuthorizationResult result = await authorizationService.AuthorizeAsync(context.User, policyName).ConfigureAwait(false);

      if (result.Succeeded)
      {
        await next(context).ConfigureAwait(false);
        return;
      }

      if (context.User.Identity?.IsAuthenticated == true)
        await context.ForbidAsync(JwtBearerDefaults.AuthenticationScheme).ConfigureAwait(false);
      else
        await context.ChallengeAsync(JwtBearerDefaults.AuthenticationScheme).ConfigureAwait(false);
    });

    return app;
  }
}
