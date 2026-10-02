using Address.Application.Dto;
using Commons.Mediator;

namespace Address.Application.Queries;

public record GetAllSettlementTypesQuery : IQuery<List<SettlementTypeDto>>;
