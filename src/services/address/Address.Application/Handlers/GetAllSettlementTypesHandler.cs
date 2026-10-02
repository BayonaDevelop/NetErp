using Address.Application.Dto;
using Address.Application.Queries;
using Address.Core.Entities;
using Address.Core.Repositories;
using Mapster;
using Commons.Mediator;

namespace Address.Application.Handlers;

public class GetAllSettlementTypesHandler(ISettlementTypeRepository repository) : IQueryHandler<GetAllSettlementTypesQuery, List<SettlementTypeDto>>
{
  private readonly ISettlementTypeRepository _repository = repository;

  public async Task<List<SettlementTypeDto>> HandleAsync(GetAllSettlementTypesQuery query, CancellationToken cancellationToken)
  {
    IEnumerable<SettlementType> entities = await _repository.GetAllSettlementTypesAsync(cancellationToken).ConfigureAwait(false);
    return [.. entities.Select(i => i.Adapt<SettlementTypeDto>())];
  }
}
