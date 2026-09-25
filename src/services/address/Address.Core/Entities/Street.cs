namespace Address.Core.Entities;

public partial class Street
{
    public long SettlementId { get; set; }

    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Address> AddressStreetAs { get; set; } = [];

    public virtual ICollection<Address> AddressStreetBs { get; set; } = [];

    public virtual ICollection<Address> AddressStreets { get; set; } = [];

    public virtual Settlement Settlement { get; set; } = null!;
}
