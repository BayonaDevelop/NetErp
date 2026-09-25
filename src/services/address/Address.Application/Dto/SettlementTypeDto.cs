namespace Address.Application.Dto;

public class SettlementTypeDto
{
  public int Id { get; set; }

  public required string Name { get; set; } = null!;

  public string? ZipCode { get; set; }
}
