using Address.Application.Dto;
using Address.Application.Queries;
using Address.Core.Entities;
using Address.Core.Repositories;
using Mapster;
using Commons.Mediator;

namespace Address.Application.Handlers;

public class GetAllLocalitiesByMunicipalityIdHandler(ILocalityRepository repository) : IQueryHandler<GetAllLocalitiesByMunicipalityIdQuery, List<LocalityDto>>
{
  private readonly ILocalityRepository _repository = repository;

  public async Task<List<LocalityDto>> HandleAsync(GetAllLocalitiesByMunicipalityIdQuery query, CancellationToken cancellationToken)
  {
    IEnumerable<Locality> entities = await _repository.GetAllLocalitiesByMunicipalityIdAsync(query.MunicipalityId, query.Name, cancellationToken).ConfigureAwait(false);
    return [.. entities.Select(i => i.Adapt<LocalityDto>())];
  }
}
