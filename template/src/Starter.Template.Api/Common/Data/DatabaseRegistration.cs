using Microsoft.EntityFrameworkCore;

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

        builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

        return builder;
    }
#else
    public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<InMemoryDatabase>();
        builder.Services.AddDbContext<AppDbContext>((provider, options) =>
            options.UseSqlite(provider.GetRequiredService<InMemoryDatabase>().ConnectionString));

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
}
