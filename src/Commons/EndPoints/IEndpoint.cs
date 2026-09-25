using Microsoft.AspNetCore.Routing;

namespace Commons.EndPoints;

public interface IEndpoint
{
  static abstract void MapEndpoints(IEndpointRouteBuilder app);
}
