using Address.Api.Endpoints;
using Address.Application.Commands;
using Address.Application.Dto;
using Address.Application.Dto.Addresses;
using Address.Application.Queries;
using Commons.Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using LocalityDto = Address.Application.Dto.Addresses.LocalityDto;
using SettlementDto = Address.Application.Dto.Addresses.SettlementDto;

namespace Address.Test.Endpoints;

[SuppressMessage("Style", "IDE0079:Remove unnecessary suppression", Justification = "La supresión CRR0029 es necesaria porque se aplica en Testing")]
[SuppressMessage("Async", "CRR0029:ConfigureAwait unnecessary", Justification = "En el caso de Testing no es necesario especificar el valor de ConfigureAwait.")]
public sealed class AddressEndpointsTests : IAsyncLifetime
{
  private readonly IDispatcher _dispatcher = Substitute.For<IDispatcher>();
  private WebApplication _app = null!;
  private HttpClient _client = null!;

  private static AddressDto CreateAddressRequest() => new()
  {
    ZipCode = "01000",
    Street = new StreetDto
    {
      Name = "Calle Principal",
      Settlement = new SettlementDto
      {
        Name = "Colonia Centro",
        Locality = new LocalityDto { MunicipalityId = 1, Name = "Centro" }
      }
    }
  };

  public async Task InitializeAsync()
  {
    using CancellationTokenSource cts = new();
    WebApplicationBuilder builder = WebApplication.CreateBuilder();
    builder.WebHost.UseTestServer();
    builder.Logging.ClearProviders();
    builder.Services.AddSingleton(_dispatcher);

    _app = builder.Build();
    AddressEndpoints.MapEndpoints(_app);

    await _app.StartAsync(cts.Token).ConfigureAwait(false);
    _client = _app.GetTestClient();
  }

  public async Task DisposeAsync()
  {
    using CancellationTokenSource cts = new();
    _client.Dispose();
    await _app.StopAsync(cts.Token).ConfigureAwait(false);
    await _app.DisposeAsync().ConfigureAwait(false);
  }

  [Fact]
  public async Task AddAddress_WhenDispatched_ReturnsOkWithNewAddressId()
  {
    using CancellationTokenSource cts = new();
    AddressDto request = CreateAddressRequest();
    _dispatcher.SendAsync(Arg.Any<AddAddressCommand>(), Arg.Any<CancellationToken>()).Returns(42L);

    HttpResponseMessage response = await _client.PostAsJsonAsync("api/v1/addresses/", request, cts.Token);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    long body = await response.Content.ReadFromJsonAsync<long>(cts.Token);
    Assert.Equal(42L, body);
  }

  [Fact]
  public async Task AddAddress_ForwardsSubmittedDataToDispatcher()
  {
    using CancellationTokenSource cts = new();
    AddressDto request = CreateAddressRequest();
    _dispatcher.SendAsync(Arg.Any<AddAddressCommand>(), Arg.Any<CancellationToken>()).Returns(1L);

    await _client.PostAsJsonAsync("api/v1/addresses/", request, cts.Token);

    await _dispatcher.Received(1).SendAsync(
      Arg.Is<AddAddressCommand>(c => c.Request.Street.Name.Equals(request.Street.Name)),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetAddressById_WhenFound_ReturnsOkWithAddress()
  {
    using CancellationTokenSource cts = new();
    AddressDto expected = CreateAddressRequest();
    _dispatcher.SendAsync(Arg.Any<GetAddressByIdQuery>(), Arg.Any<CancellationToken>()).Returns(expected);

    HttpResponseMessage response = await _client.GetAsync("api/v1/addresses/9");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    AddressDto? body = await response.Content.ReadFromJsonAsync<AddressDto>(cts.Token);
    Assert.NotNull(body);
    Assert.Equal(expected.ZipCode, body.ZipCode);
    await _dispatcher.Received(1).SendAsync(Arg.Is<GetAddressByIdQuery>(q => q.Id == 9), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetAllCountries_ReturnsOkWithCountries()
  {
    using CancellationTokenSource cts = new();
    List<CountryDto> expected = [new CountryDto { Id = 1, Name = "Mexico" }];
    _dispatcher.SendAsync(Arg.Any<GetAllCountriesQuery>(), Arg.Any<CancellationToken>()).Returns(expected);

    HttpResponseMessage response = await _client.GetAsync("api/v1/addresses/country/all");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    List<CountryDto>? body = await response.Content.ReadFromJsonAsync<List<CountryDto>>(cts.Token);
    Assert.NotNull(body);
    Assert.Single(body);
    Assert.Equal("Mexico", body[0].Name);
  }

  [Fact]
  public async Task GetCitiesByCountryId_ForwardsCountryIdAndNameToDispatcher()
  {
    using CancellationTokenSource cts = new();
    _dispatcher.SendAsync(Arg.Any<GetAllCitiesByCountryIdQuery>(), Arg.Any<CancellationToken>()).Returns([]);

    await _client.GetAsync("api/v1/addresses/country/5/city?name=Guada");

    await _dispatcher.Received(1).SendAsync(
      Arg.Is<GetAllCitiesByCountryIdQuery>(q => q.CountryId == 5 && q.Name == "Guada"),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetCitiesByCountryId_WhenNoneExist_ReturnsOkWithEmptyList()
  {
    using CancellationTokenSource cts = new();
    _dispatcher.SendAsync(Arg.Any<GetAllCitiesByCountryIdQuery>(), Arg.Any<CancellationToken>()).Returns([]);

    HttpResponseMessage response = await _client.GetAsync("api/v1/addresses/country/5/city");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    List<CityDto>? body = await response.Content.ReadFromJsonAsync<List<CityDto>>(cts.Token);
    Assert.Empty(body!);
  }

  [Fact]
  public async Task GetMunicipalitiesByCityId_ForwardsCityIdAndNameToDispatcher()
  {
    using CancellationTokenSource cts = new();
    _dispatcher.SendAsync(Arg.Any<GetAllMunicipalitiesByCityIdQuery>(), Arg.Any<CancellationToken>()).Returns([]);

    await _client.GetAsync("api/v1/addresses/city/7/municipality?name=Coyo");

    await _dispatcher.Received(1).SendAsync(
      Arg.Is<GetAllMunicipalitiesByCityIdQuery>(q => q.CityId == 7 && q.Name == "Coyo"),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetLocalitiesByMunicipalityId_ForwardsMunicipalityIdAndNameToDispatcher()
  {
    using CancellationTokenSource cts = new();
    _dispatcher.SendAsync(Arg.Any<GetAllLocalitiesByMunicipalityIdQuery>(), Arg.Any<CancellationToken>()).Returns([]);

    await _client.GetAsync("api/v1/addresses/municipality/3/locality?name=Cen");

    await _dispatcher.Received(1).SendAsync(
      Arg.Is<GetAllLocalitiesByMunicipalityIdQuery>(q => q.MunicipalityId == 3 && q.Name == "Cen"),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetAllSettlementTypes_ReturnsOkWithSettlementTypes()
  {
    using CancellationTokenSource cts = new();
    List<SettlementTypeDto> expected = [new SettlementTypeDto { Id = 1, Name = "Colonia" }];
    _dispatcher.SendAsync(Arg.Any<GetAllSettlementTypesQuery>(), Arg.Any<CancellationToken>()).Returns(expected);

    HttpResponseMessage response = await _client.GetAsync("api/v1/addresses/settlement-types");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    List<SettlementTypeDto>? body = await response.Content.ReadFromJsonAsync<List<SettlementTypeDto>>(cts.Token);
    Assert.NotNull(body);
    Assert.Single(body);
    Assert.Equal("Colonia", body[0].Name);
  }

  [Fact]
  public async Task GetSettlementsByLocalityId_ForwardsLocalityIdAndNameToDispatcher()
  {
    using CancellationTokenSource cts = new();
    _dispatcher.SendAsync(Arg.Any<GetAllSettlementsByLocalityIdQuery>(), Arg.Any<CancellationToken>()).Returns([]);

    await _client.GetAsync("api/v1/addresses/locality/4/settlement?name=Centro");

    await _dispatcher.Received(1).SendAsync(
      Arg.Is<GetAllSettlementsByLocalityIdQuery>(q => q.LocalityId == 4 && q.Name == "Centro"),
      Arg.Any<CancellationToken>());
  }
}
