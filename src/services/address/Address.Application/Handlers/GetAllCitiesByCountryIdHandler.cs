using Address.Application.Dto;
using Address.Application.Queries;
using Address.Core.Entities;
using Address.Core.Repositories;
using Mapster;
using Microsoft.Extensions.Logging;
using Commons.Mediator;

namespace Address.Application.Handlers;

public class GetAllCitiesByCountryIdHandler(ILogger<GetAllCitiesByCountryIdHandler> logger, ICityRepository repository) : IQueryHandler<GetAllCitiesByCountryIdQuery, List<CityDto>>
{
  private readonly ILogger<GetAllCitiesByCountryIdHandler> _logger = logger;
  private readonly ICityRepository repository = repository;

  public async Task<List<CityDto>> HandleAsync(GetAllCitiesByCountryIdQuery query, CancellationToken cancellationToken)
  {
    IEnumerable<City> cities = await repository.GetAllCitiesByCountryIdAsync(query.CountryId, query.Name, cancellationToken).ConfigureAwait(false);
    if (!cities.Any())
    {
      _logger.LogWarning("No se encontraron datos");
    }
    _logger.LogInformation("Todo bien");

    return [.. cities.Select(i => i.Adapt<CityDto>())];
  }
}
