namespace Address.Application.Dto;

public class CityDto
{
  public long Id { get; set; }
  public string? Iso { get; set; }
  public required string Name { get; set; }
  public string? CoatOfArms { get; set; }
}
