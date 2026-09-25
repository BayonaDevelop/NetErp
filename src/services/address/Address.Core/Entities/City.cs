namespace Address.Core.Entities;

public partial class City
{
    public int CountryId { get; set; }

    public long Id { get; set; }

    public int Code { get; set; }

    public string? Iso { get; set; }

    public string Name { get; set; } = null!;

    public string? CoatOfArms { get; set; }

    public virtual Country Country { get; set; } = null!;

    public virtual ICollection<Municipality> Municipalities { get; set; } = [];
}
