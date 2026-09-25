using Address.Core.Entities;
using Address.Core.Repositories;
using Address.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Address.Infrastructure.Repositories;

public class AddressRepository(DatabaseContext dbContext) : IAddressRepository
{
  private readonly DatabaseContext _dbContext = dbContext;

  private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
    exception.InnerException is SqlException sqlException &&
    (sqlException.Number == 2627 || sqlException.Number == 2601);

  private async Task<Locality> SaveLocalityAsync(Locality request, CancellationToken cancellationToken)
  {
    Locality? entity = await _dbContext.Localities
      .FirstOrDefaultAsync(i => i.MunicipalityId == request.MunicipalityId && i.Name.CompareTo(request.Name) == 0, cancellationToken)
      .ConfigureAwait(false);

    if (entity != null) return entity;

    entity = new()
    {
      MunicipalityId = request.MunicipalityId,
      Name = request.Name
    };

    _dbContext.Localities.Add(entity);

    try
    {
      await _dbContext
        .SaveChangesAsync(cancellationToken)
        .ConfigureAwait(false);
    }
    catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
    {
      _dbContext.Entry(entity).State = EntityState.Detached;

      entity = await _dbContext.Localities
        .FirstAsync(i => i.MunicipalityId == request.MunicipalityId && i.Name.CompareTo(request.Name) == 0, cancellationToken)
        .ConfigureAwait(false);
    }

    return entity;
  }

  private async Task<Settlement> SaveSettlementAsync(long localityId, Settlement request, CancellationToken cancellationToken)
  {
    Settlement? entity = await _dbContext.Settlements
      .FirstOrDefaultAsync(i =>
        i.LocalityId == localityId &&
        i.ZipCode!.CompareTo(request.ZipCode) == 0 &&
        i.Name.CompareTo(request.Name) == 0,
        cancellationToken
       )
      .ConfigureAwait(false);

    if (entity != null) return entity;

    entity = new()
    {
      LocalityId = localityId,
      Name = request.Name
    };

    _dbContext.Settlements.Add(entity);

    try
    {
      await _dbContext
        .SaveChangesAsync(cancellationToken)
        .ConfigureAwait(false);
    }
    catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
    {
      _dbContext.Entry(entity).State = EntityState.Detached;

      entity = await _dbContext.Settlements
        .FirstAsync(i =>
          i.LocalityId == localityId &&
          i.ZipCode!.CompareTo(request.ZipCode) == 0 &&
          i.Name.CompareTo(request.Name) == 0,
          cancellationToken
         )
        .ConfigureAwait(false);
    }

    return entity;
  }

  private async Task<long> SaveStreetAsync(Street request, long settlementId, CancellationToken cancellationToken)
  {
    Street? entity = await _dbContext.Streets
      .FirstOrDefaultAsync(i => i.SettlementId == settlementId && i.Name.CompareTo(request.Name) ==0, cancellationToken)
      .ConfigureAwait(false);

    if (entity != null) return entity.Id;

    Street streetEntity = new()
    {
      SettlementId = settlementId,
      Name = request.Name
    };

    _dbContext.Streets.Add(streetEntity);

    try
    {
      await _dbContext
        .SaveChangesAsync(cancellationToken)
        .ConfigureAwait(false);

      return streetEntity.Id;
    }
    catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
    {
      _dbContext.Entry(streetEntity).State = EntityState.Detached;

      entity = await _dbContext.Streets
        .FirstAsync(i => i.SettlementId == settlementId && i.Name.CompareTo(request.Name) ==0, cancellationToken)
        .ConfigureAwait(false);

      return entity.Id;
    }
  }

  public async Task<long> AddAddressAsync(Core.Entities.Address request, CancellationToken cancellationToken)
  {
    request.Street.Settlement.ZipCode = request.ZipCode;

    await using var transaction = await _dbContext.Database
      .BeginTransactionAsync(cancellationToken)
      .ConfigureAwait(false);

    Locality localityEntity = await SaveLocalityAsync(request.Street.Settlement.Locality, cancellationToken).ConfigureAwait(false);
    Settlement settlementEntity = await SaveSettlementAsync(localityEntity.Id, request.Street.Settlement, cancellationToken).ConfigureAwait(false);
    long streetId = await SaveStreetAsync(request.Street, settlementEntity.Id, cancellationToken).ConfigureAwait(false);
    long? streetAId = request.StreetA == null ? null : await SaveStreetAsync(request.StreetA!, settlementEntity.Id, cancellationToken).ConfigureAwait(false);
    long? streetBId = request.StreetB == null ? null : await SaveStreetAsync(request.StreetB!, settlementEntity.Id, cancellationToken).ConfigureAwait(false);

    request.Street = await _dbContext.Streets.FirstAsync(i => i.Id == streetId, cancellationToken).ConfigureAwait(false);
    request.StreetA = null;
    request.StreetB = null;
    request.StreetAid = streetAId;
    request.StreetBid = streetBId;

    _dbContext.Addresses.Add(request);
    await _dbContext
        .SaveChangesAsync(cancellationToken)
        .ConfigureAwait(false);

    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

    return request.Id;
  }

  public async Task<Core.Entities.Address> GetAddressByIdAsync(long addressId, CancellationToken cancellationToken)
  {
    var entity = await _dbContext.Addresses
      .Include(i => i.Street)
      .Include(i => i.StreetA)
      .Include(i => i.StreetB)
      .Include(i => i.Street.Settlement)
      .Include(i => i.Street.Settlement.SettlementType)
      .Include(i => i.Street.Settlement.Locality)
      .Include(i => i.Street.Settlement.Locality.Municipality)
      .Include(i => i.Street.Settlement.Locality.Municipality.City)
      .FirstOrDefaultAsync(i =>  i.Id == addressId, cancellationToken)
      .ConfigureAwait(false);
    return entity ?? new();
  }
}
