namespace Address.Core.Repositories;

public interface ISettlementTypeRepository
{
  Task<IEnumerable<Entities.SettlementType>> GetAllSettlementTypesAsync(CancellationToken cancellationToken);
}
