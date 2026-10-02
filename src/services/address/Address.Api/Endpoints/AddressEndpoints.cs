using Address.Application.Commands;
using Address.Application.Dto.Addresses;
using Address.Application.Queries;
using Commons.EndPoints;
using Commons.Mediator;

namespace Address.Api.Endpoints;

public class AddressEndpoints : IEndpoint
{
  public static void MapEndpoints(IEndpointRouteBuilder app)
  {
    RouteGroupBuilder group = app.MapGroup("api/v1/addresses").WithTags("Address");

    _ = group.MapPost("/", async (IDispatcher mediator, AddressDto request, CancellationToken cancellationToken) =>
    {
      return TypedResults.Ok(await mediator.SendAsync(new AddAddressCommand(request), cancellationToken).ConfigureAwait(false));
    });

    _ = group.MapGet("/{addressId}", async (IDispatcher mediator, Int64 addressId, CancellationToken cancellationToken) =>
    {
      return TypedResults.Ok(await mediator.SendAsync(new GetAddressByIdQuery(addressId), cancellationToken).ConfigureAwait(false));
    });

    _ = group.MapGet("/country/all", async (IDispatcher mediator, CancellationToken cancellationToken) =>
    {
      return TypedResults.Ok(await mediator.SendAsync(new GetAllCountriesQuery(), cancellationToken).ConfigureAwait(false));
    });

    _ = group.MapGet("/country/{countryId}/city", async (IDispatcher mediator, Int32 countryId, String? name, CancellationToken cancellationToken) =>
    {
      return TypedResults.Ok(await mediator.SendAsync(new GetAllCitiesByCountryIdQuery(countryId, name), cancellationToken).ConfigureAwait(false));
    });

    _ = group.MapGet("/city/{cityId}/municipality", async (IDispatcher mediator, Int64 cityId, String? name, CancellationToken cancellationToken) =>
    {
      return TypedResults.Ok(await mediator.SendAsync(new GetAllMunicipalitiesByCityIdQuery(cityId, name), cancellationToken).ConfigureAwait(false));
    });

    _ = group.MapGet("/municipality/{municipalityId}/locality", async (IDispatcher mediator, Int64 municipalityId, String? name, CancellationToken cancellationToken) =>
    {
      return TypedResults.Ok(await mediator.SendAsync(new GetAllLocalitiesByMunicipalityIdQuery(municipalityId, name), cancellationToken).ConfigureAwait(false));
    });

    _ = group.MapGet("/settlement-types", async (IDispatcher mediator, CancellationToken cancellationToken) =>
    {
      return TypedResults.Ok(await mediator.SendAsync(new GetAllSettlementTypesQuery(), cancellationToken).ConfigureAwait(false));
    });

    _ = group.MapGet("/locality/{localityId}/settlement", async (IDispatcher mediator, Int64 localityId, String? name, CancellationToken cancellationToken) =>
    {
      return TypedResults.Ok(await mediator.SendAsync(new GetAllSettlementsByLocalityIdQuery(localityId, name), cancellationToken).ConfigureAwait(false));
    });
  }
}
