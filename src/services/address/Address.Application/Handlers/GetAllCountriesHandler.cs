using Address.Application.Dto;
using Address.Application.Queries;
using Address.Core.Entities;
using Address.Core.Repositories;
using Mapster;
using Commons.Mediator;

namespace Address.Application.Handlers;

public class GetAllCountriesHandler(ICountryRepository repository) : IQueryHandler<GetAllCountriesQuery, List<CountryDto>>
{
  private readonly ICountryRepository _repository = repository;

  public async Task<List<CountryDto>> HandleAsync(GetAllCountriesQuery query, CancellationToken cancellationToken)
  {
    IEnumerable<Country> entities = await _repository.GetAllCountiesAsync(cancellationToken).ConfigureAwait(false);
    return [.. entities.Select(i => i.Adapt<CountryDto>())];
  }
}
