namespace Address.Core.Entities;

public partial class Country
{
    public int Id { get; set; }

    public int Code { get; set; }

    public string? Iso2 { get; set; }

    public string? Iso3 { get; set; }

    public string Name { get; set; } = null!;

    public string? ZipCodeRegex { get; set; }

    public string? SatRegistrationRegex { get; set; }

    public string? Region { get; set; }

    public string? CoatOfArms { get; set; }

    public string? Flag { get; set; }

    public virtual ICollection<City> Cities { get; set; } = [];
}
