using Commons.Mediator;
using FluentValidation;
using FluentValidation.Results;

namespace Commons.ExceptionHandlers;

public class CommandValidationExceptionHandler<TCommand, TResult>(
  ICommandHandler<TCommand, TResult> inner,
  IEnumerable<IValidator<TCommand>> validators
) : ICommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
{
  private readonly ICommandHandler<TCommand, TResult> _inner = inner;
  private readonly IEnumerable<IValidator<TCommand>> _validators = validators;

  public async Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken)
  {
    if (_validators.Any())
    {
      var context = new ValidationContext<TCommand>(command);
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
    return await _inner.HandleAsync(command, cancellationToken).ConfigureAwait(false);
  }
}
