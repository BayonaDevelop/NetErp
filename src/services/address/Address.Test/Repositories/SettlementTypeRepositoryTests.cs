using Address.Core.Entities;
using Address.Infrastructure.Data;
using Address.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace Address.Test.Repositories;

[SuppressMessage("Style", "IDE0079:Remove unnecessary suppression", Justification = "La supresión CRR0029 es necesaria porque se aplica en Testing")]
[SuppressMessage("Async", "CRR0029:ConfigureAwait unnecessary", Justification = "En el caso de Testing no es necesario especificar el valor de ConfigureAwait.")]
public class SettlementTypeRepositoryTests
{
  private static DatabaseContext CreateInMemoryContext(string? dbName = null)
  {
    DbContextOptions<DatabaseContext> options = new DbContextOptionsBuilder<DatabaseContext>()
      .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
      .Options;

    return new DatabaseContext(options);
  }

  [Fact]
  public async Task GetAllSettlementTypesAsync_ReturnsAllSettlementTypes()
  {
    using CancellationTokenSource cts = new();
    await using DatabaseContext context = CreateInMemoryContext();
    context.SettlementTypes.AddRange(
      new SettlementType { Name = "Colonia" },
      new SettlementType { Name = "Fraccionamiento" });
    await context.SaveChangesAsync(cts.Token);

    SettlementTypeRepository sut = new(context);

    IEnumerable<SettlementType> result = await sut.GetAllSettlementTypesAsync(cts.Token);

    Assert.Equal(2, result.Count());
    Assert.Contains(result, i => i.Name.Equals("Colonia"));
    Assert.Contains(result, i => i.Name.Equals("Fraccionamiento"));
  }

  [Fact]
  public async Task GetAllSettlementTypesAsync_WhenNoneExist_ReturnsEmpty()
  {
    using CancellationTokenSource cts = new();
    await using DatabaseContext context = CreateInMemoryContext();
    SettlementTypeRepository sut = new(context);

    IEnumerable<SettlementType> result = await sut.GetAllSettlementTypesAsync(cts.Token);

    Assert.Empty(result);
  }
}
