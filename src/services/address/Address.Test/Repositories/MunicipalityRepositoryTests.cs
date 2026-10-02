using Address.Core.Entities;
using Address.Infrastructure.Data;
using Address.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace Address.Test.Repositories;

[SuppressMessage("Style", "IDE0079:Remove unnecessary suppression", Justification = "La supresión CRR0029 es necesaria porque se aplica en Testing")]
[SuppressMessage("Async", "CRR0029:ConfigureAwait unnecessary", Justification = "En el caso de Testing no es necesario especificar el valor de ConfigureAwait.")]
public class MunicipalityRepositoryTests
{
  private static DatabaseContext CreateInMemoryContext(string? dbName = null)
  {
    DbContextOptions<DatabaseContext> options = new DbContextOptionsBuilder<DatabaseContext>()
      .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
      .Options;

    return new DatabaseContext(options);
  }

  [Fact]
  public async Task GetAllMunicipalitiesByCityIdAsync_ReturnsOnlyMunicipalitiesForThatCityOrderedByCode()
  {
    using CancellationTokenSource cts = new();
    await using DatabaseContext context = CreateInMemoryContext();
    context.Cities.Add(new City { Id = 1, CountryId = 1, Code = 1, Name = "Ciudad de Mexico" });
    context.Cities.Add(new City { Id = 2, CountryId = 1, Code = 2, Name = "Guadalajara" });
    context.Municipalities.AddRange(
      new Municipality { CityId = 1, Code = 2, Name = "Coyoacan" },
      new Municipality { CityId = 1, Code = 1, Name = "Azcapotzalco" },
      new Municipality { CityId = 2, Code = 1, Name = "Zapopan" });
    await context.SaveChangesAsync(cts.Token);

    MunicipalityRepository sut = new(context);

    List<Municipality> result = (await sut.GetAllMunicipalitiesByCityIdAsync(1, null, cts.Token)).ToList();

    Assert.Equal(2, result.Count);
    Assert.Equal("Azcapotzalco", result[0].Name);
    Assert.Equal("Coyoacan", result[1].Name);
  }

  [Fact]
  public async Task GetAllMunicipalitiesByCityIdAsync_WhenNameIsProvided_FiltersByName()
  {
    using CancellationTokenSource cts = new();
    await using DatabaseContext context = CreateInMemoryContext();
    context.Cities.Add(new City { Id = 1, CountryId = 1, Code = 1, Name = "Ciudad de Mexico" });
    context.Municipalities.AddRange(
      new Municipality { CityId = 1, Code = 1, Name = "Coyoacan" },
      new Municipality { CityId = 1, Code = 2, Name = "Azcapotzalco" });
    await context.SaveChangesAsync(cts.Token);

    MunicipalityRepository sut = new(context);

    List<Municipality> result = (await sut.GetAllMunicipalitiesByCityIdAsync(1, "Coyo", cts.Token)).ToList();

    Municipality municipality = Assert.Single(result);
    Assert.Equal("Coyoacan", municipality.Name);
  }

  [Fact]
  public async Task GetAllMunicipalitiesByCityIdAsync_WhenNoMunicipalitiesMatch_ReturnsEmpty()
  {
    using CancellationTokenSource cts = new();
    await using DatabaseContext context = CreateInMemoryContext();
    MunicipalityRepository sut = new(context);

    IEnumerable<Municipality> result = await sut.GetAllMunicipalitiesByCityIdAsync(999, null, cts.Token);

    Assert.Empty(result);
  }
}
