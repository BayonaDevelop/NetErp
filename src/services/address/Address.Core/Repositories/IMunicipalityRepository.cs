namespace Address.Core.Repositories;

public interface IMunicipalityRepository
{
  Task<IEnumerable<Entities.Municipality>> GetAllMunicipalitiesByCityIdAsync(long cityId, string? name, CancellationToken cancellationToken);
}
