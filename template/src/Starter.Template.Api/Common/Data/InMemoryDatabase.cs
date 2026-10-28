using Microsoft.Data.Sqlite;

namespace Starter.Template.Api.Common.Data;

// An in-memory SQLite database lives as long as one connection to it is open. This one stays open for the
// app's lifetime, while each DbContext opens its own connection to the same database (shared cache).
// The name is unique per instance, so test servers running side by side don't share their data.
public sealed class InMemoryDatabase : IDisposable
{
    private readonly SqliteConnection _keepAlive;

    public InMemoryDatabase()
    {
        ConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Guid.NewGuid().ToString("N"),
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared,
        }.ToString();

        _keepAlive = new SqliteConnection(ConnectionString);
        _keepAlive.Open();
    }

    public string ConnectionString { get; }

    public void Dispose() => _keepAlive.Dispose();
}
