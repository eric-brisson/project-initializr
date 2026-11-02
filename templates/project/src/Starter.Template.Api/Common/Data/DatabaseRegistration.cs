using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Starter.Template.Api.Common.Data;

public static class DatabaseRegistration
{
#if (UseDatabase)
    // The schema comes from the Flyway scripts in db/migrations, not from EF Core migrations.
    // In development, the connection string (with the password) is in the API's user secrets.
    public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("The connection string 'ConnectionStrings:Default' is missing.");

        builder.Services.AddDbContext<AppDbContext>(options => options
            .UseSqlServer(connectionString)
            .ConfigureWarnings(SaveChangesFailedAsDebug));

        // Not ready while the database can't be reached.
        builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>(tags: [Program.ReadyTag]);

        // A duplicate value in a unique index gets a 409 Conflict instead of a 500.
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

        // A duplicate value in a unique index gets a 409 Conflict instead of a 500.
        builder.Services.AddExceptionHandler<UniqueConstraintExceptionHandler>();

        return builder;
    }

    // The database starts empty each time: create its tables from the EF Core model.
    public static WebApplication CreateDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();

        return app;
    }
#endif

    // EF Core logs a failed SaveChanges as an error, with its stack trace, before the exception goes up. A duplicate
    // name would then leave an error in the logs (Splunk) for a plain 409. The exception still reaches the exception
    // handler, which logs it as an error when it isn't a unique index violation, so nothing real goes unseen.
    private static void SaveChangesFailedAsDebug(WarningsConfigurationBuilder warnings) =>
        warnings.Log((CoreEventId.SaveChangesFailed, LogLevel.Debug));
}
