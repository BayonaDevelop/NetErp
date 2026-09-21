using Commons.Mediator;
using FluentValidation;
using FluentValidation.Results;

namespace Commons.ExceptionHandlers;

public class QueryValidationExceptionHandler<TQuery, TResult>(
  IQueryHandler<TQuery, TResult> inner,
  IEnumerable<IValidator<TQuery>> validators
) : IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
{
  private readonly IQueryHandler<TQuery, TResult> _inner = inner;
  private readonly IEnumerable<IValidator<TQuery>> _validators = validators;

  public async Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken)
  {
    if (_validators.Any())
    {
      var context = new ValidationContext<TQuery>(query);
      var results = await Task
        .WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)))
        .ConfigureAwait(false);

      List<ValidationFailure> failures = [.. results.
                      SelectMany(r => r.Errors)
                      .Where(f => f != null)];

      foreach (ValidationFailure item in failures)
      {
        List<string> words = [.. item.PropertyName.Split('.')];
        foreach (var word in words)
          item.ErrorMessage = item.ErrorMessage.Replace(word, "").TrimStart();
      }

      if (failures.Count != 0)
      {
        throw new ValidationException(failures);
      }
    }
    return await _inner.HandleAsync(query, cancellationToken).ConfigureAwait(false);
  }
}
