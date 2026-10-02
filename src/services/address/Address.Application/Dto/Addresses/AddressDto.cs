namespace Address.Application.Dto.Addresses;

public class AddressDto
{
  public long Id { get; set; }
  public string? ZipCode { get; set; }

  public string? InternalNumber { get; set; }

  public string? ExternalNumber { get; set; }

  public string? Indications { get; set; }

  public required StreetDto Street { get; set; }

  public StreetDto? StreetA { get; set; }

  public StreetDto? StreetB { get; set; }
}
