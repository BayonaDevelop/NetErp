using Address.Core.Entities;
using Address.Infrastructure.Data;
using Address.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace Address.Test.Repositories;

[SuppressMessage("Style", "IDE0079:Remove unnecessary suppression", Justification = "La supresión CRR0029 es necesaria porque se aplica en Testing")]
[SuppressMessage("Async", "CRR0029:ConfigureAwait unnecessary", Justification = "En el caso de Testing no es necesario especificar el valor de ConfigureAwait.")]
public class CountryRepositoryTests
{
  private static DatabaseContext CreateInMemoryContext(string? dbName = null)
  {
    DbContextOptions<DatabaseContext> options = new DbContextOptionsBuilder<DatabaseContext>()
      .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
      .Options;

    return new DatabaseContext(options);
  }

  [Fact]
  public async Task GetAllCountiesAsync_ReturnsAllCountriesWithProjectedFields()
  {
    using CancellationTokenSource cts = new();
    await using DatabaseContext context = CreateInMemoryContext();
    context.Countries.Add(new Country { Code = 52, Iso2 = "MX", Iso3 = "MEX", Name = "Mexico", Flag = "mx.png" });
    await context.SaveChangesAsync(cts.Token);

    CountryRepository sut = new(context);

    IEnumerable<Country> result = await sut.GetAllCountiesAsync(cts.Token);

    Country country = Assert.Single(result);
    Assert.Equal("MEX", country.Iso3);
    Assert.Equal("Mexico", country.Name);
    Assert.Equal("mx.png", country.Flag);
  }

  [Fact]
  public async Task GetAllCountiesAsync_WhenNoneExist_ReturnsEmpty()
  {
    using CancellationTokenSource cts = new();
    await using DatabaseContext context = CreateInMemoryContext();
    CountryRepository sut = new(context);

    IEnumerable<Country> result = await sut.GetAllCountiesAsync(cts.Token);

    Assert.Empty(result);
  }
}
