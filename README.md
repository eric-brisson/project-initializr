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

A minimal API organized in vertical slices, with unit and integration tests (xUnit v3).

- `Starter.Template` is replaced everywhere by the name you choose (files, folders,
  namespaces, project references).
- Central package management (`Directory.Packages.props`).
- Health checks for Kubernetes probes: `/actuator/health/liveness` (no checks: the process
  answers) and `/actuator/health/readiness` (checks tagged `ready`, the database included with
  `inmemory` or `database`).
- A `UserSecretsId` on the API (a new one per project), so `dotnet user-secrets` works right
  away. In Development, user secrets override the `appsettings` files.
- Optional modules through the `modules` parameter: `scalar`, `swaggerui` (both by default),
  `inmemory`, `database`. `inmemory` and `database` both store the technologies added through
  the API with EF Core, and exclude each other (`database` wins when both are given):
  - `inmemory` uses an in-memory SQLite database, created from the EF Core model at startup
    and emptied on every restart. No Docker needed.
  - `database` uses SQL Server, with the schema in Flyway scripts (`db/migrations`). The
    integration tests run against a throwaway SQL Server started with Testcontainers, migrated
    by the same Flyway image. Without Docker they are skipped, unless `CI` is set (as on
    GitHub Actions), where they fail.

  The `database` module adds a `compose.yaml` running SQL Server (and a `flyway` service) and a
  password-less connection string in
  `appsettings.Development.json`. The database is named with `--databaseName`, by default
  the solution name without dots (`Sample.Orders` → `SampleOrders`). The CLI fills in the password (see below); with plain
  `dotnet new`, set `ConnectionStrings:Default` with `dotnet user-secrets` and
  `MSSQL_SA_PASSWORD` in a `.env` yourself, create the database, then run
  `docker compose run --rm flyway`.
  The demo enables `scalar`, `swaggerui` and `database` (see `Directory.Build.props`), so the
  repo always builds and tests them; check `inmemory` by generating a project.

- A `.starter.json` at the root records the solution name and the data module, for the
  feature template (see below).

```bash
dotnet new project-initializr -n Sample.Orders -o ./Sample.Orders
dotnet new project-initializr -n Sample.Orders -o ./Sample.Orders --modules scalar
dotnet new project-initializr -n Sample.Orders -o ./Sample.Orders --modules ""
```

## The feature template

Adds a CRUD feature to a generated solution: `Features/<Plural>` with the five slices (list,
get, create, update, delete), the entity, its DTO, the endpoint and service registrations,
and one integration test file per endpoint. The entity starts with `Id` and `Name`, and
`// TODO` comments mark where your own properties go. With a data module (`--storage efcore`),
the handlers use `AppDbContext` and an `IEntityTypeConfiguration` maps the entity; without one
(`--storage memory`), they use a singleton in-memory store.

It only adds files. Wire the feature yourself: `Map<Plural>Endpoints()` in
`Common/EndpointRegistration.cs` and `Add<Plural>Services()` in `Common/ServiceRegistration.cs`.
With SQL Server, also create the table in a new Flyway script. The feature's integration tests
fail until then.

```bash
cd Sample.Orders
dotnet new starter-feature -n Account --plural Accounts --solution Sample.Orders --storage efcore
```

## The CLI

Asks for the template, the folder, the solution name, the modules and whether to run
`git init` (unless the folder is already in a repo), then generates,
restores, builds and tests the project. The prompts and messages are in French.

The modules of the Données group (SQLite en mémoire, SQL Server) work like radio buttons:
checking one unchecks the other.

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
