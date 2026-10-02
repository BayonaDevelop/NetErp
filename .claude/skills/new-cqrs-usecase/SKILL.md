---
name: new-cqrs-usecase
description: Use this skill when the user pide agregar un caso de uso nuevo dentro de un servicio ya existente de NetErp — un Command o Query nuevo (p.ej. "agrega un comando para actualizar el email del usuario", "crea una query que liste los roles"). Genera el Command/Query + Handler + Validator + Dto siguiendo el patrón de Mediator propio en Commons y las convenciones de Identity.Application, sin registrar nada a mano en DI. NO usar para crear un servicio desde cero (ver new-service) ni para exponer el caso de uso vía HTTP (ver new-endpoint, que sí registra el endpoint).
---

# Agregar un caso de uso CQRS nuevo

Este proyecto usa un mediator propio (Commons/Mediator, no MediatR). Un
Command/Query nuevo queda funcionando con handler, validación y decoradores
de excepción **sin tocar ningún registro de DI a mano**, siempre que sigas
las convenciones de nombre/ubicación de abajo.

## 1. Decidir Command vs Query

- **Command** (`ICommand<TResponse>` + `ICommandHandler<TCommand,TResponse>`):
  para operaciones que escriben estado.
- **Query** (`IQuery<TResponse>` + `IQueryHandler<TQuery,TResponse>`): para
  lecturas.
- Excepción conocida y **deliberada**: `LoginHandler` es un
  `IQueryHandler` aunque escribe (`CreateLogginAttemptAsync`,
  `IssueRefreshTokenAsync`) porque desde el endpoint "login" se percibe como
  lectura. Si estás tocando ese código específico, mantén la consistencia en
  vez de "corregir" la pureza CQRS sin que te lo pidan. Para un caso de uso
  nuevo que no sea ese, sigue la regla normal (escribe → Command).

## 2. Ubicación de archivos

Dentro de `<Servicio>.Application/`:

```
Commands/<Verbo><Entidad>Command.cs        # o Queries/<Verbo><Entidad>Query.cs
Handlers/<Verbo><Entidad>Handler.cs
Validators/<Verbo><Entidad>Validator.cs    # opcional, solo si hay reglas que validar
Dto/<Entidad>Dto.cs                        # si el caso de uso devuelve datos
Mappers/<Entidad>Mapper.cs                 # si hace falta mapear Entity -> Dto
```

## 3. Command/Query

`record` implementando `ICommand<TResponse>` o `IQuery<TResponse>`
(`Commons/Mediator/`). Solo los datos necesarios como parámetros del record.

## 4. Handler

- Primary constructor para inyectar dependencias (`I*Repository`, etc.) —
  se resuelven solas, no hay que registrar el handler en ningún lado (scanning
  automático por assembly, ver `RegistrationOfApplicationServices<TAssemblyMarker>.AddApplication`
  en CLAUDE.md).
- `.ConfigureAwait(false)` en cada `await`.
- Si el repositorio puede no encontrar la entidad, espera un
  `Optional<T>` (no un sentinela tipo `new Entity()`) y lanza la excepción
  de dominio apropiada si `HasValue` es `false` (p.ej.
  `InvalidOperationException` con mensaje claro) — replica el patrón ya
  corregido en `UpdateUserRolesAsync`/`GetUserByIdHandler` de Identity, no el
  patrón viejo del sentinela vacío.
- Comentarios en español explicando el *por qué*, nunca el qué.

## 5. Validator (opcional)

Si el Command/Query tiene reglas de negocio o de forma que validar, crea un
`AbstractValidator<TCommand>` (FluentValidation) en el mismo assembly.
Queda envuelto automáticamente por `Command/QueryValidationExceptionHandler`
(corre antes del handler, lanza `ValidationException` si falla, que
`ValidationExceptionHandler` de Commons traduce a 400) — **no** hay que
envolver ni registrar el validator a mano.

## 6. Dto / Mapper

- Dto como `record`.
- Si el mapeo Entity→Dto no es trivial, un mapper estático en `Mappers/`.
- Mensajes de usuario (errores, validaciones) en el `.resx` `UserMessages`
  del servicio, leídos con `ResxLocalizer` — no strings hardcodeados ni
  `IStringLocalizer` inyectado.

## 7. Test

En `<Servicio>.Test/`, un archivo de test por Handler y por Validator (si
existe), xUnit + NSubstitute (mockea `I*Repository`, no una implementación
real). Naming: `Method_When<condición>_<resultado esperado>`. Cubre al
menos: caso feliz, caso de validación fallida (si hay Validator), caso de
entidad no encontrada (si aplica `Optional<T>`).

## 8. Verificar

```bash
dotnet build NetErp.slnx
dotnet test src/services/<servicio>/<Servicio>.Test/<Servicio>.Test.csproj
```

Si el caso de uso necesita exponerse por HTTP, sigue con la skill
`new-endpoint` — no mapees el endpoint a mano en `Program.cs`.
