using Address.Application.Dto.Addresses;
using Address.Application.Queries;
using Address.Core.Repositories;
using Commons.Mediator;

namespace Address.Application.Handlers;

public class GetAddressByIdHandler(IAddressRepository repository) : IQueryHandler<GetAddressByIdQuery, AddressDto>
{
  private readonly IAddressRepository _repository = repository;

  public async Task<AddressDto> HandleAsync(GetAddressByIdQuery query, CancellationToken cancellationToken)
  {
    Core.Entities.Address entity = await _repository.GetAddressByIdAsync(query.Id, cancellationToken).ConfigureAwait(false);

    SettlementDto settlement = new()
    {
      SettlementTypeId = entity.Street.Settlement.SettlementTypeId,
      Locality = new LocalityDto
      {
        CountryId = entity.Street.Settlement.Locality!.Municipality!.City!.CountryId,
        CityId = entity.Street.Settlement.Locality!.Municipality!.CityId,
        MunicipalityId = entity.Street.Settlement.Locality!.MunicipalityId,        
        Id = entity.Street.Settlement.Locality!.Id,
        Name = entity.Street.Settlement.Locality!.Name
      },
      Id = entity.Street.Settlement.Id,
      Name = entity.Street.Settlement.Name
    };

    AddressDto response = new()
    {
      Street = new()
      {
        Settlement = settlement,
        Id = entity.Street.Id,
        Name = entity.Street.Name
      },
      Id = entity.Id,
      ZipCode = entity.ZipCode,
      InternalNumber = entity.InternalNumber,
      ExternalNumber = entity.ExternalNumber,
      Indications = entity.Indications
    };

    if (entity.StreetA != null)
      response.StreetA = new()
      {
        Id = entity.StreetA.Id,
        Name = entity.StreetA.Name
      };

    if (entity.StreetB != null)
      response.StreetB = new()
      {
        Id = entity.StreetB.Id,
        Name = entity.StreetB.Name
      };

    return response;
  }
}
