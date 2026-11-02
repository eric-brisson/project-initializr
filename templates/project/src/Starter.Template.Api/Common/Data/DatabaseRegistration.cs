using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Starter.Template.Api.Common.Data;

public static class DatabaseRegistration
{
#if (UseDatabase)
    // Le schéma vient des scripts Flyway de db/migrations, pas des migrations EF Core.
    // En développement, la chaîne de connexion (avec le mot de passe) est dans les secrets utilisateur de l'API.
    public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("The connection string 'ConnectionStrings:Default' is missing.");

        builder.Services.AddDbContext<AppDbContext>(options => options
            .UseSqlServer(connectionString)
            .ConfigureWarnings(SaveChangesFailedAsDebug));

        // Pas prête tant que la base est injoignable.
        builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>(tags: [Program.ReadyTag]);

        // Une valeur en double dans un index unique donne un 409 Conflict au lieu d'un 500.
        builder.Services.AddExceptionHandler<UniqueConstraintExceptionHandler>();

        return builder;
    }
#else
    public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<InMemoryDatabase>();
        builder.Services.AddDbContext<AppDbContext>((provider, options) => options
            .UseSqlite(provider.GetRequiredService<InMemoryDatabase>().ConnectionString)
            .ConfigureWarnings(SaveChangesFailedAsDebug));

        builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>(tags: [Program.ReadyTag]);

        // Une valeur en double dans un index unique donne un 409 Conflict au lieu d'un 500.
        builder.Services.AddExceptionHandler<UniqueConstraintExceptionHandler>();

        return builder;
    }

    // La base démarre vide à chaque fois : crée ses tables à partir du modèle EF Core.
    public static WebApplication CreateDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();

        return app;
    }
#endif

    // EF Core journalise un SaveChanges en échec comme une erreur, avec sa pile d'appels, avant que l'exception remonte.
    // Un nom en double laisserait alors une erreur dans les logs (Splunk) pour un simple 409. L'exception atteint quand même
    // le gestionnaire d'exceptions, qui la journalise en erreur si ce n'est pas une violation d'index unique : rien ne passe inaperçu.
    private static void SaveChangesFailedAsDebug(WarningsConfigurationBuilder warnings) =>
        warnings.Log((CoreEventId.SaveChangesFailed, LogLevel.Debug));
}
