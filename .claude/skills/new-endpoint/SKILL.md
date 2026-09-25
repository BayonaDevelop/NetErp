---
name: new-endpoint
description: Use this skill cuando el usuario pide exponer un caso de uso vía HTTP en un servicio de NetErp — un endpoint minimal API nuevo (p.ej. "expón un endpoint para actualizar el email", "agrega el GET de roles"). Genera la clase IEndpoint que Commons descubre automáticamente vía MapEndpointsFromAssembly, sin tocar Program.cs a mano. Requiere que el Command/Query ya exista (si no, usar primero new-cqrs-usecase).
---

# Agregar un endpoint minimal API nuevo

Los endpoints se descubren y registran automáticamente vía reflection
(`app.MapEndpointsFromAssembly(assembly)`, ver `Commons/EndPoints/IEndpoint.cs`).
**No** hay que registrar el endpoint a mano en `Program.cs`.

## 1. Confirmar que el Command/Query existe

Si el caso de uso todavía no existe en `*.Application`, créalo primero con
la skill `new-cqrs-usecase` — un endpoint sin Command/Query detrás no tiene
sentido en este patrón.

## 2. Crear la clase IEndpoint

Ubicación: `<Servicio>.Api/Endpoints/` (revisa la carpeta de Identity.Api
como referencia de organización).

```csharp
public class <Verbo><Entidad>Endpoint : IEndpoint
{
  public static void MapEndpoints(IEndpointRouteBuilder app)
  {
    app.MapPost("/<recurso>", async (
      <Verbo><Entidad>Request request,
      IDispatcher dispatcher,
      CancellationToken cancellationToken) =>
    {
      var command = new <Verbo><Entidad>Command(/* ... */);
      var result = await dispatcher.SendAsync(command, cancellationToken).ConfigureAwait(false);
      return Results.Ok(result);
    });
  }
}
```

Ajusta el verbo HTTP (`MapGet`/`MapPost`/`MapPut`/`MapDelete`) y la forma de
recibir parámetros (ruta, query string, body) al caso de uso real.

## 3. Autorización

- Si el endpoint debe estar protegido, agrega `.RequireAuthorization()` (o
  una policy específica, como la policy `Swagger` que usa el Gateway para
  proteger `/swagger`/Scalar comparando el claim `Role` con el
  `NormalizedName` del rol en Identity).
- Recuerda que el JWT lo valida el **Gateway**, no cada servicio — si este
  endpoint vive detrás del Gateway, confirma que la ruta YARP correspondiente
  ya pasa por el middleware de autenticación.
- Si el endpoint (como `/login`/`/logout` en Gateway) solo debe existir en
  desarrollo, gátalo con `app.Environment.IsDevelopment()` dentro de
  `MapEndpoints` — sigue el mismo patrón que Gateway/Program.cs, no lo
  registres condicionalmente desde otro lado.

## 4. Validación y errores

No agregues manejo de errores manual en el endpoint: `ValidationException`
(desde el Validator del Command/Query, si existe) y `BadHttpRequestException`
ya se traducen a 400 vía `app.UseExceptionHandler()` +
`AddProblemDetails()` (Commons). El endpoint solo arma el Command/Query y
devuelve el resultado.

## 5. Test del endpoint

Sigue el patrón de `AuthenticationEndpointsTests`/`LoginEndpointsTests`, NO
`WebApplicationFactory` completo:

1. `WebApplication.CreateBuilder()` + `builder.WebHost.UseTestServer()`.
2. Mapear **solo** esta clase `IEndpoint` (no toda la app).
3. Sustituir `IDispatcher` con NSubstitute (`Substitute.For<IDispatcher>()`)
   configurando el `SendAsync` esperado.
4. Si el endpoint hace de proxy HTTP a otro servicio (como Gateway →
   Identity.Api), sustituye `IHttpClientFactory` con un
   `HttpMessageHandler` de prueba — reutiliza
   `Gateway.Test/TestSupport/StubHttpMessageHandler.cs` si aplica, en vez de
   intentar configurar `SendAsync` con NSubstitute directamente (es
   `protected`, NSubstitute no puede).
5. Si construyes el `WebApplicationBuilder` con
   `builder.Configuration.Sources.Clear()`, inyecta config con
   `builder.Configuration.AddInMemoryCollection(new Dictionary<string,string?> {...})`,
   nunca con el indexer (`builder.Configuration["Key"] = valor`) —
   `ConfigurationManager` lanza `InvalidOperationException` sin fuentes
   registradas.
6. Si pruebas headers de seguridad (`Strict-Transport-Security` vía
   `HstsMiddleware`), usa un `HttpClient.BaseAddress` que no sea
   `localhost`/`127.0.0.1`/`::1` (p.ej. `https://gateway.tests.local/`).

Naming: `Method_When<condición>_<resultado esperado>`.

## 6. Verificar

```bash
dotnet build NetErp.slnx
dotnet test src/services/<servicio>/<Servicio>.Test/<Servicio>.Test.csproj   # o Gateway.Test
```
