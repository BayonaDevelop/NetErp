using Address.Core.Entities;
using Address.Core.Repositories;
using Address.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Address.Infrastructure.Repositories;

public class MunicipalityRepository(DatabaseContext dbContext) : IMunicipalityRepository
{
  private readonly DatabaseContext _dbContext = dbContext;

  public async Task<IEnumerable<Municipality>> GetAllMunicipalitiesByCityIdAsync(long cityId, string? name, CancellationToken cancellationToken)
  {
    IQueryable<Municipality> query = from municipality in _dbContext.Municipalities
                join city in _dbContext.Cities on municipality.CityId equals city.Id
                where city.Id == cityId
                orderby municipality.Code
                select new Municipality { 
                  Id = municipality.Id,
                  Iso = municipality.Iso,
                  Name = municipality.Name
                };

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
