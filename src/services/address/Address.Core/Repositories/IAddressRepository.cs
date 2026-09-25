namespace Address.Core.Repositories;

public interface IAddressRepository
{
  Task<long> AddAddressAsync(Entities.Address request, CancellationToken cancellationToken);

  Task<Entities.Address> GetAddressByIdAsync(long addressId, CancellationToken cancellationToken);
}
