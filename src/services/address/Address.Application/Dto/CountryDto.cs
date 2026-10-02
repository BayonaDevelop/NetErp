namespace Address.Application.Dto;

public class CountryDto
{
  public int Id { get; set; }
  public string? Iso3 { get; set; }
  public required string Name { get; set; }
  public string? Flag { get; set; }
}
