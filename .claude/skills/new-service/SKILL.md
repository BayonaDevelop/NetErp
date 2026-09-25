---
name: new-service
description: Use this skill when the user asks to create a new microservice in NetErp (por ejemplo "implementa address", "crea el servicio de inventario", "agrega un microservicio nuevo"). Genera desde cero la estructura Api/Application/Core/Infrastructure replicando el patrón de Identity, registra los proyectos en NetErp.slnx y deja el servicio listo para exponerse vía Gateway. NO usar para agregar un caso de uso o endpoint dentro de un servicio ya existente (ver las skills new-cqrs-usecase y new-endpoint para eso).
---

# Crear un microservicio nuevo en NetErp

`Identity` es el único servicio implementado end-to-end y es la plantilla a
replicar. `src/services/address/` está vacío a propósito (scaffold
eliminado): si el pedido es sobre `address`, es un servicio nuevo desde cero,
no hay nada que recuperar.

No copies literalmente archivos de Identity (arrastrarías namespaces,
`UserSecretsId` y lógica específica de autenticación); usa Identity como
referencia de **estructura y convenciones**, escribiendo cada archivo nuevo
para el dominio del servicio pedido.

## 1. Confirmar el nombre y alcance

Si el nombre del servicio no es explícito, pregunta antes de generar
archivos. Usa PascalCase para el nombre del servicio (p.ej. `Address`,
`Inventory`) y minúsculas para la carpeta (`src/services/address/`).

## 2. Estructura de carpetas y proyectos

Crear, bajo `src/services/<nombre-minusculas>/`:

```
<Name>.Core/            # entidades + interfaces I*Repository. CERO ProjectReference (ni a Commons).
<Name>.Application/     # Commands/ Queries/ Handlers/ Validators/ Dto/ Mappers/
<Name>.Infrastructure/  # EF Core SqlServer + repositorios *Repository
<Name>.Api/             # minimal API, Program.cs, endpoints IEndpoint
<Name>.Test/            # xUnit + NSubstitute
```

Referencias de proyecto (igual que Identity, ver diagrama en CLAUDE.md):

- `<Name>.Application` → `<Name>.Core` + `Commons`
- `<Name>.Infrastructure` → `<Name>.Application`
- `<Name>.Api` → `<Name>.Infrastructure`
- `<Name>.Test` → `<Name>.Api` + `<Name>.Infrastructure`

`<Name>.Core` no referencia nada, ni siquiera Commons — es intencional en la
arquitectura (dominio sin dependencias), no solo una peculiaridad de
Identity.

Cada `.csproj` nuevo debe tener `Nullable` + `ImplicitUsings` habilitados y
target `net10.0`, igual que los existentes (revisa un `.csproj` de Identity
como referencia de shape, pero fija las versiones de paquete tú mismo — no
hay `Directory.Build.props` que las centralice).

## 3. Core

- Entidades como clases planas del dominio.
- Interfaces `I*Repository` con métodos async devolviendo `Task<T>` o, para
  búsquedas que pueden no encontrar nada, `Task<Optional<T>>`.
- Si necesitas el patrón "no encontrado" sin sentinela mágico, **replica**
  el struct `Optional<T>` (ver `Identity.Core/Types/Optional.cs`) dentro del
  `*.Core` de este servicio — no lo importes de Identity.Core ni lo muevas a
  Commons; esa separación es deliberada.

## 4. Application

- Un `Command`/`Query` por caso de uso, como `record` implementando
  `ICommand<TResponse>` / `IQuery<TResponse>` (`Commons/Mediator/`).
- Handler correspondiente implementando `ICommandHandler<TCommand,TResponse>`
  / `IQueryHandler<TQuery,TResponse>`, con primary constructor para inyectar
  dependencias, `.ConfigureAwait(false)` en cada `await`.
- Validators opcionales con FluentValidation (`AbstractValidator<T>`) en el
  mismo assembly — quedan envueltos automáticamente por los decoradores de
  validación, no hay que registrarlos a mano (ver
  `RegistrationOfApplicationServices<TAssemblyMarker>.AddApplication`).
- DTOs como `record`.
- Mensajes de usuario en un `.resx` propio del servicio (patrón
  `UserMessages`, ver I18n en CLAUDE.md), leído con `ResxLocalizer`.
- Comentarios de código en español, explicando el *por qué*, no el qué.

Para el primer caso de uso concreto, usa la skill `new-cqrs-usecase` en vez
de escribirlo a mano aquí.

## 5. Infrastructure

- Repositorios EF Core SqlServer nombrados `<Entity>Repository`
  implementando `I<Entity>Repository` — el registro en DI es automático por
  convención de nombre (`RegistrationOfInfrastructureServices<TAssemblyMarker>.RepositoryRegistry`).
  Si un servicio no sigue esa convención de nombre, hay que registrarlo a
  mano; evita esa situación si no es necesaria.
- `DbContext` propio del servicio (no reutilices el de Identity).

## 6. Api

- Endpoints como clases `IEndpoint` (usa la skill `new-endpoint` para
  generarlos) — se descubren solos vía `MapEndpointsFromAssembly`.
- `Program.cs` debe incluir, siguiendo el patrón de Identity.Api:
  - `builder.AddObservability("<name>")` + `app.UseObservability()`.
  - `builder.AddKestrelHardening()` + `app.UseKestrelHardening()`.
  - Registro de Application/Infrastructure por
    `RegistrationOfApplicationServices<TAssemblyMarker>.AddApplication` (u
    homólogo de Infrastructure).
  - `app.UseExceptionHandler()` + `AddProblemDetails()` para traducir
    `ValidationException`/`BadHttpRequestException` a 400.
  - `app.MapEndpointsFromAssembly(assembly)`.
  - **Terminar el archivo con `public partial class Program { }`** — sin
    esto `WebApplicationFactory<Program>` no funciona en los tests.
- Configuración de `ConnectionStrings` vía user-secrets, no hardcodeada en
  `appsettings.json`.

## 7. Test

- Un archivo de test por Handler/Validator/Repository, naming
  `Method_When<condición>_<resultado esperado>`.
- xUnit + NSubstitute, sin mocks manuales.
- Para tests de endpoints: NO usar `WebApplicationFactory` completo; armar
  `WebApplication.CreateBuilder()` + `UseTestServer()` mapeando solo la
  clase `IEndpoint` bajo prueba (ver `AuthenticationEndpointsTests` en
  Identity.Test como referencia). Reservar `WebApplicationFactory<Program>`
  para un `ProgramTests` que valide la composición completa.

## 8. Integrar con la solución

1. Agregar los 5 proyectos nuevos a `NetErp.slnx`.
2. Si el servicio debe exponerse externamente, agregar su cluster/ruta en la
   configuración de YARP del Gateway (revisar cómo Gateway enruta a
   `identity.api` como referencia) — NO dupliques la política `Swagger`
   salvo que el nuevo servicio también deba compartir el `UserSecretsId` del
   JWT (por ahora solo Gateway e Identity lo comparten; evalúa si este
   servicio nuevo debe emitir tokens o solo consumirlos vía Gateway).
3. Agregar el contenedor del servicio a `docker-compose.yml` (puerto interno
   `:8080`, variables OTLP apuntando a `otel-collector`, nunca directo a
   Zipkin/Prometheus/Grafana) y su override en `docker-compose.debug.yml` si
   se necesita debug remoto.

## 9. Verificar

```bash
dotnet build NetErp.slnx
dotnet test src/services/<nombre-minusculas>/<Name>.Test/<Name>.Test.csproj
dotnet test NetErp.slnx
```

No marques la tarea como terminada si `dotnet build` o los tests fallan.
