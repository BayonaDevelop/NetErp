using Address.Core.Entities;
using Address.Infrastructure.Data;
using Address.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace Address.Test.Repositories;

[SuppressMessage("Style", "IDE0079:Remove unnecessary suppression", Justification = "La supresión CRR0029 es necesaria porque se aplica en Testing")]
[SuppressMessage("Async", "CRR0029:ConfigureAwait unnecessary", Justification = "En el caso de Testing no es necesario especificar el valor de ConfigureAwait.")]
public class SettlementRepositoryTests
{
  private static DatabaseContext CreateInMemoryContext(string? dbName = null)
  {
    DbContextOptions<DatabaseContext> options = new DbContextOptionsBuilder<DatabaseContext>()
      .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
      .Options;

    return new DatabaseContext(options);
  }

  [Fact]
  public async Task GetAllSettlementsAsync_ReturnsOnlySettlementsForThatLocality()
  {
    using CancellationTokenSource cts = new();
    await using DatabaseContext context = CreateInMemoryContext();
    context.Settlements.AddRange(
      new Settlement { LocalityId = 1, SettlementTypeId = 1, Name = "Centro" },
      new Settlement { LocalityId = 2, SettlementTypeId = 1, Name = "Norte" });
    await context.SaveChangesAsync(cts.Token);

    SettlementRepository sut = new(context);

    IEnumerable<Settlement> result = await sut.GetAllSettlementsAsync(1, null, cts.Token);

    Settlement settlement = Assert.Single(result);
    Assert.Equal("Centro", settlement.Name);
  }

  [Fact]
  public async Task GetAllSettlementsAsync_WhenNameIsProvided_FiltersByName()
  {
    using CancellationTokenSource cts = new();
    await using DatabaseContext context = CreateInMemoryContext();
    context.Settlements.AddRange(
      new Settlement { LocalityId = 1, SettlementTypeId = 1, Name = "Centro" },
      new Settlement { LocalityId = 1, SettlementTypeId = 1, Name = "Norte" });
    await context.SaveChangesAsync(cts.Token);

    SettlementRepository sut = new(context);

    IEnumerable<Settlement> result = await sut.GetAllSettlementsAsync(1, "Nor", cts.Token);

    Settlement settlement = Assert.Single(result);
    Assert.Equal("Norte", settlement.Name);
  }

  [Fact]
  public async Task GetAllSettlementsAsync_WhenNoneMatch_ReturnsEmpty()
  {
    using CancellationTokenSource cts = new();
    await using DatabaseContext context = CreateInMemoryContext();
    SettlementRepository sut = new(context);

    IEnumerable<Settlement> result = await sut.GetAllSettlementsAsync(999, null, cts.Token);

    Assert.Empty(result);
  }
}
