using Address.Application.Dto;
using Commons.Mediator;

namespace Address.Application.Queries;

public record GetAllCitiesByCountryIdQuery(Int32 CountryId, String? Name) : IQuery<List<CityDto>>;
