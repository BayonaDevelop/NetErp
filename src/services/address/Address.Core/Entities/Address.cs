namespace Address.Core.Entities;

public partial class Address
{
  public long StreetId { get; set; }

  public long? StreetAid { get; set; }

  public long? StreetBid { get; set; }

  public long Id { get; set; }

  public string? ZipCode { get; set; }

  public string? InternalNumber { get; set; }

  public string? ExternalNumber { get; set; }

  public string? Indications { get; set; }

  public virtual Street Street { get; set; } = null!;

  public virtual Street? StreetA { get; set; }

  public virtual Street? StreetB { get; set; }
}
