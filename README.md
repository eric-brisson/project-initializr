# Project Initializr

A Spring Initializr-style starter for .NET 10 APIs: a `dotnet new` template that is also a
working demo solution, a template that adds features to it, and an interactive CLI that drives both.

```
templates/project/    dotnet new project template (short name: project-initializr)
templates/feature/    dotnet new item template for a CRUD feature (short name: starter-feature)
tools/Starter.Init/   Spectre.Console CLI (command: starter-init)
```

Install (or, after a change, reinstall with `--force`) both templates at once:

```bash
dotnet new install ./templates
```

## The project template

A minimal API organized in vertical slices, with unit and integration tests (xUnit v3). It holds two
features: `About` (`GET /api/v1/about`, the API's version and the components it's built with) and
`Technologies`, a CRUD example that is also the source of the feature template (see below).

- `Starter.Template` is replaced everywhere by the name you choose (files, folders,
  namespaces, project references).
- Central package management (`Directory.Packages.props`).
- Shared build settings (`Directory.Build.props`): the target framework, nullable, the .NET analyzers at
  their recommended level and the `.editorconfig` style rules checked by the build. Warnings fail the build.
- Errors in the RFC 9457 problem details format: validation failures, error status codes returned without a
  body (404...) and exceptions (`UseExceptionHandler`). With a data module, a unique index violation
  gets a 409 Conflict (`UniqueConstraintExceptionHandler`) instead of a 500.
- A `Dockerfile` for the API and a `compose.yaml` that builds and runs it: `docker compose up --build`.
- A `README.md` for the generated solution, written for the selected modules.
- Health checks for Kubernetes probes: `/actuator/health/liveness` (no checks: the process
  answers) and `/actuator/health/readiness` (checks tagged `ready`, the database included with
  `inmemory` or `database`).
- A `UserSecretsId` on the API (a new one per project), so `dotnet user-secrets` works right
  away. In Development, user secrets override the `appsettings` files.
- Optional modules through the `modules` parameter: `scalar`, `swaggerui` (both by default),
  `inmemory`, `database`, `telemetry`. `inmemory` and `database` both store the technologies added through
  the API with EF Core, and exclude each other (`database` wins when both are given):
  - `inmemory` uses an in-memory SQLite database, created from the EF Core model at startup
    and emptied on every restart. No Docker needed. Names compare case-insensitively (`NOCASE`),
    as with SQL Server.
  - `database` uses SQL Server, with the schema in Flyway scripts (`db/migrations`). The
    integration tests run against a throwaway SQL Server started with Testcontainers, migrated
    by the same Flyway image. Without Docker they are skipped, unless `CI` is set (as on
    GitHub Actions), where they fail.

  The `database` module adds SQL Server (and a `flyway` service) to `compose.yaml`, and a
  password-less connection string in
  `appsettings.Development.json`. The database is named with `--databaseName`, by default
  the solution name without dots (`Sample.Orders` → `SampleOrders`). The CLI fills in the password (see below); with plain
  `dotnet new`, set `ConnectionStrings:Default` with `dotnet user-secrets` and
  `MSSQL_SA_PASSWORD` in a `.env` yourself, start SQL Server with `docker compose up -d sqlserver`,
  create the database, then run `docker compose run --rm flyway`.
  The demo enables `scalar`, `swaggerui`, `database` and `telemetry` (see `Directory.Build.props`), so the
  repo always builds and tests them; check `inmemory` by generating a project.

- The `telemetry` module sends the API's traces, metrics and logs to a local Aspire dashboard
  (`docker compose up -d dashboard`, then <http://localhost:18888>), with stable OpenTelemetry packages:
  ASP.NET Core, HttpClient, runtime and, with `database`, SqlClient (EF Core's queries included).
  The SDK is registered only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set, which the launch profiles
  and `compose.yaml` do. Deployed, it isn't set, so nothing doubles what Dynatrace OneAgent records.
  The CLI starts the dashboard after generating the project. Launched from a launch profile (F5,
  `dotnet run`), the API also starts it when nothing listens on port 4317 (`DashboardLauncher`, enabled by
  `Telemetry__StartDashboard` in the launch profiles), in the background; without Docker, it logs a warning.
  No slice has telemetry code: `AppTelemetry` holds the source and meter for custom spans and metrics,
  with a comment on when they're worth writing.
  `launchSettings.json` can't hold the template's comment markers (`dotnet run` rejects comments
  there), so it comes in two copies: `launchSettings.json` with the variable, and
  `launchSettings.notelemetry.json` without it, renamed when the module isn't selected. Keep them in sync.

- A `.starter.json` at the root records the solution name and the data module, for the
  feature template (see below).

```bash
dotnet new project-initializr -n Sample.Orders -o ./Sample.Orders
dotnet new project-initializr -n Sample.Orders -o ./Sample.Orders --modules scalar
dotnet new project-initializr -n Sample.Orders -o ./Sample.Orders --modules ""
```

## The feature template

Adds a CRUD feature to a generated solution: `Features/<Plural>` with one file per slice (list,
get, create, update, delete), the entity (`<Name>.cs`), its DTO (`<Name>Dto.cs`, whose `From` method
is the one mapping from the entity), the feature file that registers its services and routes
(`<Plural>Feature.cs`), and one integration test file per endpoint. The entity starts with `Id` and a
unique `Name`, and `// TODO` comments mark where your own properties go. With a data module
(`--data sqlite` or `--data sqlserver`), the handlers use `AppDbContext`, and the entity file also
holds its EF Core mapping; without one (`--data none`), they use a singleton in-memory store, with
its unit tests.

The template has no files of its own: it reads the `Technologies` feature of the project template
(`templates/feature/.template.config/template.json`, `sources`), and renames `Technology`,
`Technologies` and the `technologies` route. A change to that feature reaches both templates, and
the repo's tests cover it. Keep its comments free of the lowercase words, which would be renamed too,
and wrap a comment meant only for generated features in `//#if (IsFeature)`.

It only adds files. Wire the feature yourself: `Map<Plural>Endpoints()` in
`Common/EndpointRegistration.cs` and `Add<Plural>Services()` in `Common/ServiceRegistration.cs`.
With SQL Server, also create the table in a new Flyway script. The feature's integration tests
fail until then.

```bash
cd Sample.Orders
dotnet new starter-feature -n Account --plural Accounts --solution Sample.Orders --data sqlserver
```

## The CLI

Asks for the template, the folder, the solution name, the modules and whether to run
`git init` (unless the folder is already in a repo), then generates,
restores, builds and tests the project. The prompts and messages are in French. The questions live in
`Ask.cs`: each one is drawn under its badge, aligned with it, and only the answer stays once it's given. The
lists (● one choice, ◼ several) are drawn there, since Spectre.Console's selection prompts can't be indented
and use a `>` marker; free text uses Spectre's `TextPrompt`.

The modules are asked step by step, one question per category, in the order they are declared in
`Templates.cs`: the API documentation (several boxes to check), the data storage (one choice, with
« Aucune base de données » first) and the local telemetry (yes or no). A category declares its badge, its
question and whether it allows only one choice; a category with a single module is asked as yes or no.

With the database module, it also asks for the database name and the sa password, starts
SQL Server in Docker, creates the database, applies the Flyway migrations and shows how to connect. The password is stored twice: the full
connection string in the API's user secrets (overriding the one in
`appsettings.Development.json`), and `MSSQL_SA_PASSWORD` in a gitignored `.env` for
`docker compose`. The password goes to `dotnet user-secrets` through stdin, never as an
argument.

```bash
dotnet run --project tools/Starter.Init/src/Starter.Init
```

For scripts, pass the name (and optionally the folder) to skip every prompt:

```bash
dotnet run --project tools/Starter.Init/src/Starter.Init -- Sample.Orders ./Sample.Orders
```

To add a feature, run it from the generated solution (or a subfolder) and choose « Ajouter une
feature », or pass `feature` and the name. It reads `.starter.json` to match the data module,
asks for the name and its plural, generates and builds, then prints the lines to wire and where
they go (and, with SQL Server, the next Flyway script to write). It doesn't run the tests, since
they fail until the feature is wired.

```bash
starter-init feature Account
```

To install it as a .NET tool:

```bash
dotnet pack tools/Starter.Init/src/Starter.Init -o tools/Starter.Init/nupkg
dotnet tool install --global Starter.Init --add-source tools/Starter.Init/nupkg
```

New templates and modules are declared in `tools/Starter.Init/src/Starter.Init/Templates.cs`.

## License

MIT
