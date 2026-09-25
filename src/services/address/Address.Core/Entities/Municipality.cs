namespace Address.Core.Entities;

public partial class Municipality
{
    public long CityId { get; set; }

    public long Id { get; set; }

    public int Code { get; set; }

    public string? Iso { get; set; }

    public string Name { get; set; } = null!;

    public virtual City City { get; set; } = null!;

    public virtual ICollection<Locality> Localities { get; set; } = [];
}
