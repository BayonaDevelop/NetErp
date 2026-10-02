using Address.Core.Entities;
using Address.Infrastructure.Data;
using Address.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace Address.Test.Repositories;

[SuppressMessage("Style", "IDE0079:Remove unnecessary suppression", Justification = "La supresión CRR0029 es necesaria porque se aplica en Testing")]
[SuppressMessage("Async", "CRR0029:ConfigureAwait unnecessary", Justification = "En el caso de Testing no es necesario especificar el valor de ConfigureAwait.")]
public class CityRepositoryTests
{
  private static DatabaseContext CreateInMemoryContext(string? dbName = null)
  {
    DbContextOptions<DatabaseContext> options = new DbContextOptionsBuilder<DatabaseContext>()
      .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
      .Options;

    return new DatabaseContext(options);
  }

  [Fact]
  public async Task GetAllCitiesByCountryIdAsync_ReturnsOnlyCitiesForThatCountry()
  {
    using CancellationTokenSource cts = new();
    await using DatabaseContext context = CreateInMemoryContext();
    context.Cities.AddRange(
      new City { CountryId = 1, Code = 1, Iso = "MX-CMX", Name = "Ciudad de Mexico" },
      new City { CountryId = 2, Code = 2, Iso = "US-NY", Name = "New York" });
    await context.SaveChangesAsync(cts.Token);

    CityRepository sut = new(context);

    IEnumerable<City> result = await sut.GetAllCitiesByCountryIdAsync(1, null, cts.Token);

    City city = Assert.Single(result);
    Assert.Equal("Ciudad de Mexico", city.Name);
  }

  [Fact]
  public async Task GetAllCitiesByCountryIdAsync_WhenNameIsProvided_FiltersByName()
  {
    using CancellationTokenSource cts = new();
    await using DatabaseContext context = CreateInMemoryContext();
    context.Cities.AddRange(
      new City { CountryId = 1, Code = 1, Name = "Ciudad de Mexico" },
      new City { CountryId = 1, Code = 2, Name = "Guadalajara" });
    await context.SaveChangesAsync(cts.Token);

    CityRepository sut = new(context);

    IEnumerable<City> result = await sut.GetAllCitiesByCountryIdAsync(1, "Guada", cts.Token);

    City city = Assert.Single(result);
    Assert.Equal("Guadalajara", city.Name);
  }

  [Fact]
  public async Task GetAllCitiesByCountryIdAsync_WhenNoCitiesMatch_ReturnsEmpty()
  {
    using CancellationTokenSource cts = new();
    await using DatabaseContext context = CreateInMemoryContext();
    CityRepository sut = new(context);

    IEnumerable<City> result = await sut.GetAllCitiesByCountryIdAsync(999, null, cts.Token);

    Assert.Empty(result);
  }
}
