using Address.Core.Entities;
using Address.Core.Repositories;
using Address.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Address.Infrastructure.Repositories;

public class LocalityRepository(DatabaseContext dbContext) : ILocalityRepository
{
  private readonly DatabaseContext _dbContext = dbContext;

  public async Task<Int64> AddLocalityAsync(Locality newEntity, CancellationToken cancellationToken)
  {
    _dbContext.Localities.Add(newEntity);
    await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    return newEntity.Id;
  }

  public async Task<IEnumerable<Locality>> GetAllLocalitiesByMunicipalityIdAsync(long municipalityId, string? name, CancellationToken cancellationToken)
  {
    IQueryable<Locality> query = _dbContext.Localities
      .Where(i => i.MunicipalityId == municipalityId)
      .Select(i => new Locality
      {
        Id = i.Id,
        Name = i.Name
      });

    if (!name.IsNullOrEmpty())
    {
      query = query.Where(i => i.Name.Contains(name!));
    }

    var list = await query
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return list;
  }
}
