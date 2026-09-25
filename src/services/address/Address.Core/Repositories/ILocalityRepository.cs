namespace Address.Core.Repositories;

public interface ILocalityRepository
{
  Task<Int64> AddLocalityAsync(Entities.Locality newEntity, CancellationToken cancellationToken);

  Task<IEnumerable<Entities.Locality>> GetAllLocalitiesByMunicipalityIdAsync(long municipalityId, string? name, CancellationToken cancellationToken);
}
