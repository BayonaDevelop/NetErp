using Address.Application.Dto;
using Address.Application.Queries;
using Address.Core.Entities;
using Address.Core.Repositories;
using Mapster;
using Commons.Mediator;

namespace Address.Application.Handlers;

public class GetAllSettlementsByLocalityIdHandler(ISettlementRepository repository) : IQueryHandler<GetAllSettlementsByLocalityIdQuery, List<SettlementDto>>
{
  private readonly ISettlementRepository _repository = repository;

  public async Task<List<SettlementDto>> HandleAsync(GetAllSettlementsByLocalityIdQuery query, CancellationToken cancellationToken)
  {
    IEnumerable<Settlement> entities = await _repository.GetAllSettlementsAsync(query.LocalityId, query.Name, cancellationToken).ConfigureAwait(false);
    return [.. entities.Select(i => i.Adapt<SettlementDto>())];
  }
}
