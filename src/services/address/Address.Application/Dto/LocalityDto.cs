namespace Address.Application.Dto;

public class LocalityDto
{
  public long Id { get; set; }
  public required string Name { get; set; }

  public long MunicipalityId { get; set; }
}
