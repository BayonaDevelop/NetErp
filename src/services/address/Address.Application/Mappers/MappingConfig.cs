using Address.Application.Dto;
using Address.Core.Entities;
using Mapster;

namespace Address.Application.Mappers;

public static class MappingConfig
{
  public static void Register()
  {
    TypeAdapterConfig<Country, CountryDto>.NewConfig()
    .Map(dest => dest.Iso3, src => src.Iso3)
    .Map(dest => dest.Id, src => src.Id)
    .Map(dest => dest.Name, src => src.Name)
    .Map(dest => dest.Flag, src => src.Flag);

    TypeAdapterConfig<City, CityDto>.NewConfig()
      .Map(dest => dest.Id, src => src.Id)
      .Map(dest => dest.Iso, src => src.Iso)
      .Map(dest => dest.Name, src => src.Name)
      .Map(dest => dest.CoatOfArms, src => src.CoatOfArms);

    TypeAdapterConfig<Municipality, MunicipalityDto>.NewConfig()
      .Map(dest => dest.Id, src => src.Id)
      .Map(dest => dest.Iso, src => src.Iso)
      .Map(dest => dest.Name, src => src.Name);

    TypeAdapterConfig<Locality, Dto.Addresses.LocalityDto>.NewConfig()
      .Map(dest => dest.MunicipalityId, src => src.MunicipalityId)
      .Map(dest => dest.Id, src => src.Id)
      .Map(dest => dest.Name, src => src.Name);

    TypeAdapterConfig<SettlementType, SettlementTypeDto>.NewConfig()
      .Map(dest => dest.Id, src => src.Id)
      .Map(dest => dest.Name, src => src.Name);


  }
}
