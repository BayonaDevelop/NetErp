using Identity.Infrastructure.Data;

namespace Identity.Test.Data;

public class SqlServerDbContextTests
{
  /// <summary>
  /// El constructor sin parametros lo usan las herramientas de diseno de EF
  /// (dotnet ef migrations/database update); en tiempo de ejecucion la app
  /// siempre usa el constructor con DbContextOptions.
  /// </summary>
  [Fact]
  public void ParameterlessConstructor_CreatesInstance()
  {
    using SqlServerDbContext context = new();

    Assert.NotNull(context);
  }
}
