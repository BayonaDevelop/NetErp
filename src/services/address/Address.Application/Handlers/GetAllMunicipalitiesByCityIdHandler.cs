using Address.Application.Dto;
using Address.Application.Queries;
using Address.Core.Entities;
using Address.Core.Repositories;
using Mapster;
using Commons.Mediator;

namespace Address.Application.Handlers;

public class GetAllMunicipalitiesByCityIdHandler(IMunicipalityRepository repository) : IQueryHandler<GetAllMunicipalitiesByCityIdQuery, List<MunicipalityDto>>
{
  private readonly IMunicipalityRepository _repository = repository;

  public async Task<List<MunicipalityDto>> HandleAsync(GetAllMunicipalitiesByCityIdQuery query, CancellationToken cancellationToken)
  {
    IEnumerable<Municipality> entities = await _repository.GetAllMunicipalitiesByCityIdAsync(query.CityId, query.Name, cancellationToken).ConfigureAwait(false);
    return [.. entities.Select(i => i.Adapt<MunicipalityDto>())];
  }
}
