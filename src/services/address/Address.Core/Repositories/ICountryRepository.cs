namespace Address.Core.Repositories;

public interface ICountryRepository
{
  Task<IEnumerable<Entities.Country>> GetAllCountiesAsync(CancellationToken cancellationToken);
}
