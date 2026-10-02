using Address.Application.Dto.Addresses;
using Commons.Mediator;

namespace Address.Application.Queries;

public record GetAddressByIdQuery(long Id) : IQuery<AddressDto>;
