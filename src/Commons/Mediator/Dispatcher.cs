using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Commons.Mediator;

public sealed class Dispatcher(IServiceProvider serviceProvider) : IDispatcher
{
  private readonly IServiceProvider _serviceProvider = serviceProvider;

  private static readonly ConcurrentDictionary<Type, MethodInfo> _commandHandlerMethods = new();
  private static readonly ConcurrentDictionary<Type, MethodInfo> _queryHandlerMethods = new();

  public Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
  {
    var method = _commandHandlerMethods.GetOrAdd(command.GetType(), commandType =>
    {
      var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, typeof(TResponse));
      return handlerType.GetMethod(nameof(ICommandHandler<,>.HandleAsync))!;
    });

    var handler = _serviceProvider.GetRequiredService(method.DeclaringType!);
    return (Task<TResponse>)method.Invoke(handler, [command, cancellationToken])!;
  }

  public Task<TResponse> SendAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
  {
    var method = _queryHandlerMethods.GetOrAdd(query.GetType(), queryType =>
    {
      var handlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, typeof(TResponse));
      return handlerType.GetMethod(nameof(IQueryHandler<,>.HandleAsync))!;
    });

    var handler = _serviceProvider.GetRequiredService(method.DeclaringType!);
    return (Task<TResponse>)method.Invoke(handler, [query, cancellationToken])!;
  }
}
