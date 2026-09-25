using Commons.Mediator;
using Serilog;

namespace Commons.ExceptionHandlers;

public class QueryUnhandledExceptionHandler<TQuery, TResult>(
  IQueryHandler<TQuery, TResult> inner,
  ILogger logger
) : IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
{
  private readonly IQueryHandler<TQuery, TResult> _inner = inner;
  private readonly ILogger _logger = logger.ForContext<QueryUnhandledExceptionHandler<TQuery, TResult>>();

  public async Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken)
  {
    try
    {
      return await _inner.HandleAsync(query, cancellationToken).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
      _logger.Error(ex, "Unhandled exception occurred while handling {QueryName}. Query: {@Query}.", typeof(TQuery).Name, query);
      throw;
    }
  }
}
