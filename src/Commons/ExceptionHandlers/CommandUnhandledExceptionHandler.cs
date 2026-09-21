using Commons.Mediator;
using Serilog;

namespace Commons.ExceptionHandlers;

public class CommandUnhandledExceptionHandler<TCommand, TResult>(
  ICommandHandler<TCommand, TResult> inner,
  ILogger logger
) : ICommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
{
  private readonly ICommandHandler<TCommand, TResult> _inner = inner;
  private readonly ILogger _logger = logger.ForContext<CommandUnhandledExceptionHandler<TCommand, TResult>>();

  public async Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken)
  {
    try
    {
      return await _inner.HandleAsync(command, cancellationToken).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
      _logger.Error(ex, "Unhandled exception occurred while handling {CommandName}. Command: {@Command}.", typeof(TCommand).Name, command);
      throw;
    }
  }
}
