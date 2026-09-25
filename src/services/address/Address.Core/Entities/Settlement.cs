namespace Address.Core.Entities;

public partial class Settlement
{
    public int SettlementTypeId { get; set; }

    public long LocalityId { get; set; }

    public long Id { get; set; }

    public int Code { get; set; }

    public string Name { get; set; } = null!;

    public string? ZipCode { get; set; }

    public virtual Locality Locality { get; set; } = null!;

    public virtual SettlementType SettlementType { get; set; } = null!;

    public virtual ICollection<Street> Streets { get; set; } = [];
}
