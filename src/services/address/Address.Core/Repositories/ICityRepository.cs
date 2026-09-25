namespace Address.Core.Repositories;

public interface ICityRepository
{
  Task<IEnumerable<Entities.City>> GetAllCitiesByCountryIdAsync(int countryId, string? name, CancellationToken cancellationToken);
}
