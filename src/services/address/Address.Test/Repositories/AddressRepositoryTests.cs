using Address.Infrastructure.Data;
using Address.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using AddressEntity = Address.Core.Entities.Address;
using CityEntity = Address.Core.Entities.City;
using CountryEntity = Address.Core.Entities.Country;
using LocalityEntity = Address.Core.Entities.Locality;
using MunicipalityEntity = Address.Core.Entities.Municipality;
using SettlementEntity = Address.Core.Entities.Settlement;
using SettlementTypeEntity = Address.Core.Entities.SettlementType;
using StreetEntity = Address.Core.Entities.Street;

namespace Address.Test.Repositories;

[SuppressMessage("Style", "IDE0079:Remove unnecessary suppression", Justification = "La supresión CRR0029 es necesaria porque se aplica en Testing")]
[SuppressMessage("Async", "CRR0029:ConfigureAwait unnecessary", Justification = "En el caso de Testing no es necesario especificar el valor de ConfigureAwait.")]
public class AddressRepositoryTests
{
  private static DatabaseContext CreateInMemoryContext(string? dbName = null)
  {
    DbContextOptions<DatabaseContext> options = new DbContextOptionsBuilder<DatabaseContext>()
      .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
      .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
      .Options;

    return new DatabaseContext(options);
  }

  private static AddressEntity CreateAddressRequest(string localityName = "Centro", string settlementName = "Colonia Centro", string streetName = "Calle Principal") => new()
  {
    ZipCode = "01000",
    ExternalNumber = "123",
    InternalNumber = "A",
    Indications = "Entre calles",
    Street = new StreetEntity
    {
      Name = streetName,
      Settlement = new SettlementEntity
      {
        Name = settlementName,
        Locality = new LocalityEntity
        {
          MunicipalityId = 1,
          Name = localityName
        }
      }
    }
  };

  [Fact]
  public async Task AddAddressAsync_WhenLocalitySettlementAndStreetAreNew_CreatesAllAndReturnsAddressId()
  {
    using CancellationTokenSource cts = new();
    await using DatabaseContext context = CreateInMemoryContext();
    AddressRepository sut = new(context);
    AddressEntity request = CreateAddressRequest();

    long addressId = await sut.AddAddressAsync(request, cts.Token);

    Assert.True(addressId > 0);
    LocalityEntity locality = await context.Localities.SingleAsync(cts.Token);
    Assert.Equal("Centro", locality.Name);
    SettlementEntity settlement = await context.Settlements.SingleAsync(cts.Token);
    Assert.Equal("Colonia Centro", settlement.Name);
    Assert.Equal(locality.Id, settlement.LocalityId);
    StreetEntity street = await context.Streets.SingleAsync(cts.Token);
    Assert.Equal("Calle Principal", street.Name);
    Assert.Equal(settlement.Id, street.SettlementId);
  }

  [Fact]
  public async Task AddAddressAsync_WhenLocalityAlreadyExists_ReusesExistingLocalityInsteadOfDuplicatingIt()
  {
    using CancellationTokenSource cts = new();
    await using DatabaseContext context = CreateInMemoryContext();
    LocalityEntity existingLocality = new() { MunicipalityId = 1, Name = "Centro" };
    context.Localities.Add(existingLocality);
    await context.SaveChangesAsync(cts.Token);

    AddressRepository sut = new(context);
    AddressEntity request = CreateAddressRequest();

    await sut.AddAddressAsync(request, cts.Token);

    List<LocalityEntity> localities = await context.Localities.ToListAsync(cts.Token);
    LocalityEntity onlyLocality = Assert.Single(localities);
    Assert.Equal(existingLocality.Id, onlyLocality.Id);
  }

  [Fact]
  public async Task AddAddressAsync_WhenSettlementAlreadyExists_ReusesExistingSettlementInsteadOfDuplicatingIt()
  {
    using CancellationTokenSource cts = new();
    await using DatabaseContext context = CreateInMemoryContext();
    LocalityEntity existingLocality = new() { MunicipalityId = 1, Name = "Centro" };
    context.Localities.Add(existingLocality);
    await context.SaveChangesAsync(cts.Token);
    SettlementEntity existingSettlement = new() { LocalityId = existingLocality.Id, Name = "Colonia Centro", ZipCode = "01000" };
    context.Settlements.Add(existingSettlement);
    await context.SaveChangesAsync(cts.Token);

    AddressRepository sut = new(context);
    AddressEntity request = CreateAddressRequest();

    await sut.AddAddressAsync(request, cts.Token);

    List<SettlementEntity> settlements = await context.Settlements.ToListAsync(cts.Token);
    SettlementEntity onlySettlement = Assert.Single(settlements);
    Assert.Equal(existingSettlement.Id, onlySettlement.Id);
  }

  [Fact]
  public async Task AddAddressAsync_WhenStreetAlreadyExists_ReusesExistingStreetInsteadOfDuplicatingIt()
  {
    using CancellationTokenSource cts = new();
    await using DatabaseContext context = CreateInMemoryContext();
    LocalityEntity existingLocality = new() { MunicipalityId = 1, Name = "Centro" };
    context.Localities.Add(existingLocality);
    await context.SaveChangesAsync(cts.Token);
    SettlementEntity existingSettlement = new() { LocalityId = existingLocality.Id, Name = "Colonia Centro", ZipCode = "01000" };
    context.Settlements.Add(existingSettlement);
    await context.SaveChangesAsync(cts.Token);
    StreetEntity existingStreet = new() { SettlementId = existingSettlement.Id, Name = "Calle Principal" };
    context.Streets.Add(existingStreet);
    await context.SaveChangesAsync(cts.Token);

    AddressRepository sut = new(context);
    AddressEntity request = CreateAddressRequest();

    await sut.AddAddressAsync(request, cts.Token);

    List<StreetEntity> streets = await context.Streets.ToListAsync(cts.Token);
    StreetEntity onlyStreet = Assert.Single(streets);
    Assert.Equal(existingStreet.Id, onlyStreet.Id);
  }

  [Fact]
  public async Task AddAddressAsync_WithStreetAAndStreetB_PersistsBothCrossStreetsOnTheSameSettlement()
  {
    using CancellationTokenSource cts = new();
    await using DatabaseContext context = CreateInMemoryContext();
    AddressRepository sut = new(context);
    AddressEntity request = CreateAddressRequest();
    request.StreetA = new StreetEntity { Name = "Calle A" };
    request.StreetB = new StreetEntity { Name = "Calle B" };

    long addressId = await sut.AddAddressAsync(request, cts.Token);

    AddressEntity stored = await context.Addresses
      .Include(i => i.Street)
      .Include(i => i.StreetA)
      .Include(i => i.StreetB)
      .SingleAsync(i => i.Id == addressId, cts.Token);
    Assert.NotNull(stored.StreetA);
    Assert.NotNull(stored.StreetB);
    Assert.Equal("Calle A", stored.StreetA!.Name);
    Assert.Equal("Calle B", stored.StreetB!.Name);
    Assert.Equal(stored.Street.SettlementId, stored.StreetA!.SettlementId);
    Assert.Equal(stored.Street.SettlementId, stored.StreetB!.SettlementId);
  }

  [Fact]
  public async Task GetAddressByIdAsync_WhenAddressExists_ReturnsItWithFullGraphIncluded()
  {
    // AddAddressAsync no copia SettlementTypeId al crear un Settlement nuevo (ver
    // AddressRepository.SaveSettlementAsync), así que para ejercitar el Include completo
    // de GetAddressByIdAsync (que requiere un SettlementType real por ser una relación
    // requerida) se siembra el grafo directamente en vez de pasar por AddAddressAsync.
    using CancellationTokenSource cts = new();
    await using DatabaseContext context = CreateInMemoryContext();
    CountryEntity country = new() { Code = 52, Name = "Mexico" };
    CityEntity city = new() { Code = 1, Name = "Ciudad de Mexico", Country = country };
    MunicipalityEntity municipality = new() { Code = 1, Name = "Cuauhtemoc", City = city };
    LocalityEntity locality = new() { Name = "Centro", Municipality = municipality };
    SettlementTypeEntity settlementType = new() { Name = "Colonia" };
    SettlementEntity settlement = new() { Name = "Colonia Centro", Locality = locality, SettlementType = settlementType };
    StreetEntity street = new() { Name = "Calle Principal", Settlement = settlement };
    AddressEntity address = new() { ZipCode = "01000", Street = street };
    context.Addresses.Add(address);
    await context.SaveChangesAsync(cts.Token);

    AddressRepository sut = new(context);

    AddressEntity result = await sut.GetAddressByIdAsync(address.Id, cts.Token);

    Assert.Equal(address.Id, result.Id);
    Assert.Equal("Calle Principal", result.Street.Name);
    Assert.Equal("Colonia Centro", result.Street.Settlement.Name);
    Assert.Equal("Centro", result.Street.Settlement.Locality.Name);
    Assert.Equal("Cuauhtemoc", result.Street.Settlement.Locality.Municipality.Name);
    Assert.Equal("Ciudad de Mexico", result.Street.Settlement.Locality.Municipality.City.Name);
  }

  [Fact]
  public async Task GetAddressByIdAsync_WhenAddressDoesNotExist_ReturnsEmptyAddressSentinel()
  {
    using CancellationTokenSource cts = new();
    await using DatabaseContext context = CreateInMemoryContext();
    AddressRepository sut = new(context);

    AddressEntity result = await sut.GetAddressByIdAsync(999, cts.Token);

    Assert.Equal(0, result.Id);
  }
}
