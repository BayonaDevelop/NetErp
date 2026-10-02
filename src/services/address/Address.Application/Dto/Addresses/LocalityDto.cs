namespace Address.Application.Dto.Addresses;

public class LocalityDto
{
  public long CountryId { get; set; }
  public long CityId { get; set; }
  public long MunicipalityId { get; set; }
  public long Id { get; set; }
  public required string Name { get; set; }
}
