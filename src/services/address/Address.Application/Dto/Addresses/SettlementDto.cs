namespace Address.Application.Dto.Addresses;

public class SettlementDto
{
  public int SettlementTypeId { get; set; }
  public long Id { get; set; }
  public required string Name { get; set; }
  public required LocalityDto Locality { get; set; }
}
