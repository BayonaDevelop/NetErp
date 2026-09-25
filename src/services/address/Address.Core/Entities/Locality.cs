namespace Address.Core.Entities;

public partial class Locality
{
    public long MunicipalityId { get; set; }

    public long Id { get; set; }

    public int Code { get; set; }

    public string Name { get; set; } = null!;

    public bool IsUrban { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public virtual Municipality Municipality { get; set; } = null!;

    public virtual ICollection<Settlement> Settlements { get; set; } = [];
}
