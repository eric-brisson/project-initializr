using Microsoft.Data.Sqlite;

namespace Starter.Template.Api.Common.Data;

// Une base SQLite en mémoire existe tant qu'une connexion vers elle reste ouverte. Celle-ci reste ouverte pendant toute
// la vie de l'application, et chaque DbContext ouvre sa propre connexion vers la même base (cache partagé).
// Le nom est unique par instance : des serveurs de test lancés en parallèle ne partagent pas leurs données.
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
