namespace Address.Application.Dto.Addresses;

public class StreetDto
{
  // Solo la calle principal trae Settlement: AddressRepository.AddAddressAsync
  // resuelve Locality/Settlement a partir de Street.Settlement; para StreetA/StreetB
  // solo usa el Name.
  public SettlementDto? Settlement { get; set; }
  public long Id { get; set; }
  public required string Name { get; set; }
}
