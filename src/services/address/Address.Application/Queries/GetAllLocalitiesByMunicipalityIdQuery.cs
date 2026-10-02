using Address.Application.Dto;
using Commons.Mediator;

namespace Address.Application.Queries;

public record GetAllLocalitiesByMunicipalityIdQuery(Int64 MunicipalityId, string? Name) : IQuery<List<LocalityDto>>;
