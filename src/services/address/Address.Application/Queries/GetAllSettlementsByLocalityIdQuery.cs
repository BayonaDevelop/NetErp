using Address.Application.Dto;
using Commons.Mediator;

namespace Address.Application.Queries;

public record GetAllSettlementsByLocalityIdQuery(long LocalityId, string? Name) : IQuery<List<SettlementDto>>;
