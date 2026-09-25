using Address.Core.Entities;
using Address.Core.Repositories;
using Address.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Address.Infrastructure.Repositories;

public class CityRepository(DatabaseContext dbContext) : ICityRepository
{
  private readonly DatabaseContext _dbContext = dbContext;

  public async Task<IEnumerable<City>> GetAllCitiesByCountryIdAsync(int countryId, string? name, CancellationToken cancellationToken)
  {
    IQueryable<City> query = _dbContext.Cities
      .Where(i => i.CountryId == countryId);

    if (!name.IsNullOrEmpty())
    {
      query = query.Where(i => i.Name.Contains(name!));
    }

    var list = await query
      .Select(i => new City { 
        Id = i.Id, 
        Iso = i.Iso, 
        Name = i.Name,
        CoatOfArms = i.CoatOfArms,
      })
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return list;
  }
}
