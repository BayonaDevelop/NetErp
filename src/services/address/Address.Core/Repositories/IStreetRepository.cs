namespace Address.Core.Repositories;

public interface IStreetRepository
{
  Task<IEnumerable<Entities.Street>> GetAllStreetsAsync(long settlementId, string? name, CancellationToken cancellationToken = default);
}
