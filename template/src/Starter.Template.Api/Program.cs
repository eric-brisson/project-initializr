#if (UseScalar)
using Scalar.AspNetCore;
#endif
using Starter.Template.Api.Common;
#if (UseDatabase || UseInMemory)
using Starter.Template.Api.Common.Data;
#endif

namespace Starter.Template.Api;

public class Program
{
    public static void Main(string[] args)
    {
        // Configuration sources, each overriding the previous ones: appsettings.json, appsettings.{Environment}.json,
        // user secrets (Development only: dotnet user-secrets set <key> <value>), environment variables, command line.
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddAuthorization();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        // Error responses (validation failures included) use the RFC 9457 problem details format.
        builder.Services.AddProblemDetails();

        // Validates minimal API parameters annotated with data annotations ([Required], [StringLength], ...).
        builder.Services.AddValidation();

        builder.Services.AddFeatures();
#if (UseDatabase || UseInMemory)

        builder.AddDatabase();
#endif

        var app = builder.Build();
#if (UseInMemory)

        app.CreateDatabase();
#endif

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
#if (UseScalar)
            app.MapScalarApiReference();
#endif
#if (UseSwaggerUi)
            app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "v1"));
#endif
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapEndpoints();

        app.Run();
    }
}
