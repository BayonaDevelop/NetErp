using Address.Application.Dto;
using Commons.Mediator;

namespace Address.Application.Queries;

public record GetAllMunicipalitiesByCityIdQuery(Int64 CityId, string? Name) : IQuery<List<MunicipalityDto>>;
