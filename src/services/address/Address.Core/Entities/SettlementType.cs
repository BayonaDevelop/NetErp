namespace Address.Core.Entities;

public partial class SettlementType
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Settlement> Settlements { get; set; } = [];
}
