using Address.Core.Entities;
using Address.Core.Repositories;
using Address.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Address.Infrastructure.Repositories;

public class CountryRepository(DatabaseContext dbContext) : ICountryRepository
{
  private readonly DatabaseContext _dbContext = dbContext;

  public async Task<IEnumerable<Country>> GetAllCountiesAsync(CancellationToken cancellationToken)
  {
    var list = await _dbContext.Countries
      .Select(i => new Country
      {
        Id = i.Id,
        Iso3 = i.Iso3,
        Name = i.Name,
        Flag = i.Flag,
      })
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return list;
  }
}
