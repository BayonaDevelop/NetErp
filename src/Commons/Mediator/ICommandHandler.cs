namespace Commons.Mediator;

public interface ICommandHandler<in TCommand, TResponse> where TCommand : ICommand<TResponse>
{
  Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
