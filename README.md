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
- Optional modules through the `modules` parameter: `scalar`, `swaggerui` (both by default),
  `database`. The `database` module adds a `compose.yaml` running SQL Server, a
  `UserSecretsId` on the API and a password-less connection string in
  `appsettings.Development.json`. The database is named with `--databaseName`, by default
  the solution name without dots (`Sample.Orders` → `SampleOrders`). The CLI fills in the password (see below); with plain
  `dotnet new`, set `ConnectionStrings:Default` with `dotnet user-secrets` and
  `MSSQL_SA_PASSWORD` in a `.env` yourself.
  The demo enables every module (see `Directory.Build.props`), so the repo always builds and
  tests all of them.

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

With the database module, it also asks for the database name and the sa password, starts
SQL Server in Docker, creates the database and shows how to connect. The password is stored twice: the full
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
