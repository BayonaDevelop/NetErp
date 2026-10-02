namespace Address.Application.Dto;

public class MunicipalityDto
{
  public long Id { get; set; }

  public string? Iso { get; set; }

  public required string Name { get; set; }
}
