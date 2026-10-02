using Address.Core.Entities;
using Address.Core.Repositories;
using Address.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Address.Infrastructure.Repositories;

public class SettlementTypeRepository(DatabaseContext dbContext) : ISettlementTypeRepository
{
  private readonly DatabaseContext _dbContext = dbContext;

  public async Task<IEnumerable<SettlementType>> GetAllSettlementTypesAsync(CancellationToken cancellationToken)
  {
    IEnumerable<SettlementType> data = await _dbContext.SettlementTypes.ToListAsync(cancellationToken).ConfigureAwait(false);
    return data;
  }
}
