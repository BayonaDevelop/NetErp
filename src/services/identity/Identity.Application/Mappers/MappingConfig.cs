using Identity.Application.Dto.Responses;
using Identity.Core.Entities;
using Mapster;

namespace Identity.Application.Mappers;

public static class MappingConfig
{
  public static void RegisterMappings()
  {
    TypeAdapterConfig<User, UserResponseDto>
        .NewConfig()
        .Map(dest => dest.Roles, src => src.Roles.Select(r => r.NormalizedName).ToList());
  }
}
