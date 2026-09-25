namespace Identity.Core.Types;

/// <summary>
/// Representa un valor que puede estar presente o ausente. Reemplaza el
/// patron de "entidad vacia" (p.ej. `entity ?? new()`) que usaban los
/// repositorios para señalar "no encontrado" sin usar null.
/// </summary>
public readonly struct Optional<T>
{
  private readonly T _value;

  public bool HasValue { get; }

  private Optional(T value, bool hasValue)
  {
    _value = value;
    HasValue = hasValue;
  }

  public T Value => HasValue
    ? _value
    : throw new InvalidOperationException($"Optional<{typeof(T).Name}> no tiene valor.");

  public static Optional<T> Some(T value) => new(value, true);

  public static Optional<T> None() => new(default!, false);

  public T GetValueOrDefault(T fallback) => HasValue ? _value : fallback;
}
