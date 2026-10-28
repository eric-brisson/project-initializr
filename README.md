# Project Initializr

A Spring Initializr-style starter for .NET 10 APIs: a `dotnet new` template that is also a
working demo solution, and an interactive CLI that drives it.

```
template/             dotnet new template (short name: starter-api)
tools/Starter.Init/   Spectre.Console CLI (command: starter-init)
```

## The template

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

```bash
dotnet new install ./template
dotnet new starter-api -n Sample.Orders -o ./Sample.Orders
dotnet new starter-api -n Sample.Orders -o ./Sample.Orders --modules scalar
dotnet new starter-api -n Sample.Orders -o ./Sample.Orders --modules ""
```

After changing the template, reinstall it with `dotnet new install ./template --force`.

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

To install it as a .NET tool:

```bash
dotnet pack tools/Starter.Init/src/Starter.Init -o tools/Starter.Init/nupkg
dotnet tool install --global Starter.Init --add-source tools/Starter.Init/nupkg
```

New templates and modules are declared in `tools/Starter.Init/src/Starter.Init/Templates.cs`.

## License

MIT
