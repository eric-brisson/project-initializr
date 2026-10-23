#if (UseScalar)
using Scalar.AspNetCore;
#endif
using Starter.Template.Api.Common;

namespace Starter.Template.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddAuthorization();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

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
