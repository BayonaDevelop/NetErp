namespace Address.Core.Repositories;

public interface ISettlementRepository
{
  Task<IEnumerable<Entities.Settlement>> GetAllSettlementsAsync(long localityId, string? name, CancellationToken cancellationToken = default);
}
