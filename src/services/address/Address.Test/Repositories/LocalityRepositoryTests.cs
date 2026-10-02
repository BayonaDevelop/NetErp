using Address.Core.Entities;
using Address.Infrastructure.Data;
using Address.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace Address.Test.Repositories;

[SuppressMessage("Style", "IDE0079:Remove unnecessary suppression", Justification = "La supresión CRR0029 es necesaria porque se aplica en Testing")]
[SuppressMessage("Async", "CRR0029:ConfigureAwait unnecessary", Justification = "En el caso de Testing no es necesario especificar el valor de ConfigureAwait.")]
public class LocalityRepositoryTests
{
  private static DatabaseContext CreateInMemoryContext(string? dbName = null)
  {
    DbContextOptions<DatabaseContext> options = new DbContextOptionsBuilder<DatabaseContext>()
      .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
      .Options;

    return new DatabaseContext(options);
  }

  [Fact]
  public async Task AddLocalityAsync_WhenDataIsValid_PersistsAndReturnsId()
  {
    using CancellationTokenSource cts = new();
    await using DatabaseContext context = CreateInMemoryContext();
    LocalityRepository sut = new(context);
    Locality newEntity = new() { MunicipalityId = 1, Name = "Centro" };

    long id = await sut.AddLocalityAsync(newEntity, cts.Token);

    Locality stored = await context.Localities.SingleAsync(i => i.Id == id, cts.Token);
    Assert.Equal("Centro", stored.Name);
    Assert.Equal(1, stored.MunicipalityId);
  }

  [Fact]
  public async Task GetAllLocalitiesByMunicipalityIdAsync_ReturnsOnlyLocalitiesForThatMunicipality()
  {
    using CancellationTokenSource cts = new();
    await using DatabaseContext context = CreateInMemoryContext();
    context.Localities.AddRange(
      new Locality { MunicipalityId = 1, Name = "Centro" },
      new Locality { MunicipalityId = 2, Name = "Norte" });
    await context.SaveChangesAsync(cts.Token);

    LocalityRepository sut = new(context);

    IEnumerable<Locality> result = await sut.GetAllLocalitiesByMunicipalityIdAsync(1, null, cts.Token);

    Locality locality = Assert.Single(result);
    Assert.Equal("Centro", locality.Name);
  }

  [Fact]
  public async Task GetAllLocalitiesByMunicipalityIdAsync_WhenNameIsProvided_FiltersByName()
  {
    using CancellationTokenSource cts = new();
    await using DatabaseContext context = CreateInMemoryContext();
    context.Localities.AddRange(
      new Locality { MunicipalityId = 1, Name = "Centro" },
      new Locality { MunicipalityId = 1, Name = "Norte" });
    await context.SaveChangesAsync(cts.Token);

    LocalityRepository sut = new(context);

    IEnumerable<Locality> result = await sut.GetAllLocalitiesByMunicipalityIdAsync(1, "Nor", cts.Token);

    Locality locality = Assert.Single(result);
    Assert.Equal("Norte", locality.Name);
  }

  [Fact]
  public async Task GetAllLocalitiesByMunicipalityIdAsync_WhenNoneMatch_ReturnsEmpty()
  {
    using CancellationTokenSource cts = new();
    await using DatabaseContext context = CreateInMemoryContext();
    LocalityRepository sut = new(context);

    IEnumerable<Locality> result = await sut.GetAllLocalitiesByMunicipalityIdAsync(999, null, cts.Token);

    Assert.Empty(result);
  }
}
