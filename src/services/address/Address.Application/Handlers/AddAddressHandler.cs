using Address.Application.Commands;
using Address.Core.Repositories;
using Mapster;
using Commons.Mediator;

namespace Address.Application.Handlers;

public class AddAddressHandler(IAddressRepository repository) : ICommandHandler<AddAddressCommand, long>
{
  private readonly IAddressRepository _repository = repository;

  public Task<long> HandleAsync(AddAddressCommand command, CancellationToken cancellationToken)
  {
    Core.Entities.Address newEntity = command.Request.Adapt<Core.Entities.Address>();
    newEntity.Street.Settlement.Locality.MunicipalityId = command.Request.Street.Settlement!.Locality.MunicipalityId;
    return Task.Run(() => _repository.AddAddressAsync(newEntity, cancellationToken), cancellationToken);
  }
}
