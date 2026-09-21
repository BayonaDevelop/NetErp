using Commons.Mediator;
using FluentValidation;
using Identity.Application;
using Identity.Application.Commands;
using Identity.Application.Dto.Requests;
using Identity.Application.Dto.Responses;
using Identity.Application.Queries;
using Identity.Application.Settings;
using Identity.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Identity.Test;

public class RegistryOfServicesTests
{
  [Fact]
  public void AddApplication_RegistersDispatcherHandlersValidatorsAndJwtOptions()
  {
    ServiceCollection services = new();
    Jwt jwt = new() { SigningKey = "key", Issuer = "issuer", Audience = "audience", DurationInMinutes = 30 };

    services.AddApplication(Options.Create(jwt));

    Assert.Contains(services, d => d.ServiceType == typeof(IDispatcher));
    Assert.Contains(services, d => d.ServiceType == typeof(IPasswordHasher<User>));
    Assert.Contains(services, d => d.ServiceType == typeof(ICommandHandler<CreateUserCommand, bool>));
    Assert.Contains(services, d => d.ServiceType == typeof(IQueryHandler<LoginQuery, LoginResponseDto>));
    Assert.Contains(services, d => d.ServiceType == typeof(IValidator<CreateUserRequestDto>));
    Assert.Contains(services, d => d.ServiceType == typeof(IValidator<LoginQuery>));

    using ServiceProvider provider = services.BuildServiceProvider();
    Jwt resolved = provider.GetRequiredService<IOptions<Jwt>>().Value;

    Assert.Equal(jwt.SigningKey, resolved.SigningKey);
    Assert.Equal(jwt.Issuer, resolved.Issuer);
    Assert.Equal(jwt.Audience, resolved.Audience);
    Assert.Equal(jwt.DurationInMinutes, resolved.DurationInMinutes);
  }
}
