# Starter.Template

A .NET 10 minimal API organized in vertical slices, generated with project-initializr.

## Prerequisites

- The .NET 10 SDK.
<!--#if (UseDatabase) -->
- Docker, for SQL Server and the integration tests.
<!--#else -->
- Docker, only to run the API in a container.
<!--#endif -->

## Run the API

```bash
dotnet run --project src/Starter.Template.Api
```

<!--#if (UseScalar || UseSwaggerUi) -->
In Development, the API documentation is served at:

<!--#if (UseScalar) -->
- `/scalar` for the Scalar API reference.
<!--#endif -->
<!--#if (UseSwaggerUi) -->
- `/swagger` for Swagger UI.
<!--#endif -->
- `/openapi/v1.json` for the OpenAPI document.

<!--#endif -->
`src/Starter.Template.Api/Starter.Template.Api.http` holds ready-made requests for the VS Code REST Client
and the Visual Studio or Rider HTTP clients.

## Test

```bash
dotnet test
```

<!--#if (UseDatabase) -->
The integration tests start a throwaway SQL Server with Testcontainers and migrate it with Flyway.
Without Docker they are skipped, except when the `CI` variable is set, as on GitHub Actions, where they fail.

<!--#endif -->
## Endpoints

| Route | Purpose |
| --- | --- |
| `GET /api/v1/about` | The API's name and version, and the main components it's built with |
| `/api/v1/technologies` | The example feature: list, get, create, update and delete |
| `GET /actuator/health/liveness` | Liveness probe: the process answers |
| `GET /actuator/health/readiness` | Readiness probe: every check tagged `ready` passes |

Errors use the RFC 9457 problem details format: validation failures (400), unknown ids (404),
duplicate names (409) and unexpected exceptions (500, the details only in the logs).

<!--#if (UseDatabase) -->
## Database

SQL Server runs in Docker, and Flyway applies the scripts of `db/migrations`. EF Core reads and writes the
data, but doesn't create the schema: a new table or column goes in a new script, `V<n>__<description>.sql`.
Never edit a script once applied.

```bash
docker compose up -d sqlserver        # start SQL Server
docker compose run --rm flyway        # apply the migrations
```

The sa password is in `.env` (not committed), for docker compose. The full connection string is in the API's
user secrets, which override `appsettings.Development.json` in Development:

```bash
dotnet user-secrets list --project src/Starter.Template.Api
```

<!--#endif -->
<!--#if (UseInMemory) -->
## Database

EF Core stores the data in an in-memory SQLite database, created from the EF Core model when the API starts
and emptied when it stops. No Docker needed.

<!--#endif -->
## Run in a container

```bash
docker compose up --build
```

The API answers on <http://localhost:8080> with the Production settings, so without the API documentation.
Its image is built from `src/Starter.Template.Api/Dockerfile`.

## Project structure

```
src/Starter.Template.Api/
  Program.cs                The host: services, middleware, health probes
  Common/                   What the features share: endpoint and service registration
<!--#if (UseEfCore) -->
  Common/Data/              AppDbContext and the database setup
<!--#endif -->
  Features/<Feature>/       One folder per feature, one file per endpoint (a vertical slice)
tests/
  Starter.Template.Api.UnitTests/
  Starter.Template.Api.IntegrationTests/   The API in memory (WebApplicationFactory)
<!--#if (UseDatabase) -->
db/migrations/              The Flyway scripts
<!--#endif -->
```

A slice holds its request, its handler and its route. It reaches the data directly (EF Core or the feature's
store), without a repository or mediator layer. Each feature maps its routes in `<Feature>Endpoints.cs` and
registers its services in `<Feature>Services.cs`, both called from `Common/`.

## Add a feature

From the solution folder, with the starter-init tool:

```bash
starter-init feature Account
```

Or with the template directly:

```bash
<!--#if (UseDatabase) -->
dotnet new starter-feature -n Account --plural Accounts --solution Starter.Template --data sqlserver
<!--#elseif (UseInMemory) -->
dotnet new starter-feature -n Account --plural Accounts --solution Starter.Template --data sqlite
<!--#else -->
dotnet new starter-feature -n Account --plural Accounts --solution Starter.Template --data none
<!--#endif -->
```

It generates the same five slices and tests as the Technologies feature. Then wire it in
`Common/EndpointRegistration.cs` and `Common/ServiceRegistration.cs`,
<!--#if (UseDatabase) -->
create its table in a new Flyway script,
<!--#endif -->
and fill in the `TODO` comments.

## Conventions

- Package versions are set once, in `Directory.Packages.props`.
- `Directory.Build.props` sets the target framework and the code analysis for every project.
  Warnings fail the build, and `.editorconfig` holds the style rules the build checks.
- `.starter.json` tells starter-init which data module the solution uses.
