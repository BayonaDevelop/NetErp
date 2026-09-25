using Address.Application.Dto.Addresses;
using Commons.Mediator;

namespace Address.Application.Commands;

public record AddAddressCommand(AddressDto Request) : ICommand<long>;
