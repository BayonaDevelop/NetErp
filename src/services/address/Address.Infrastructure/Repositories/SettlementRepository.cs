using Address.Core.Entities;
using Address.Core.Repositories;
using Address.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Address.Infrastructure.Repositories;

public class SettlementRepository(DatabaseContext dbContext) : ISettlementRepository
{
  private readonly DatabaseContext _dbContext = dbContext;

  public async Task<IEnumerable<Settlement>> GetAllSettlementsAsync(long localityId, string? name, CancellationToken cancellationToken = default)
  {
    IQueryable<Settlement> query = _dbContext.Settlements.Where(i => i.LocalityId == localityId);
    
    if (!name.IsNullOrEmpty())
    {
      query = query.Where(i => i.Name.Contains(name!));
    }

    IEnumerable<Settlement> result = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    return result;
  }
}
