# The API image. The build context is the solution folder, for Directory.Build.props and Directory.Packages.props:
#   docker build -f src/Starter.Template.Api/Dockerfile -t starter-template-api .
# or with compose, from the solution folder: docker compose up --build api

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# The project file first: the restore layer stays cached until a dependency changes.
COPY Directory.Build.props Directory.Packages.props global.json .editorconfig ./
COPY src/Starter.Template.Api/Starter.Template.Api.csproj src/Starter.Template.Api/
RUN dotnet restore src/Starter.Template.Api/Starter.Template.Api.csproj

COPY src/ src/
RUN dotnet publish src/Starter.Template.Api/Starter.Template.Api.csproj --no-restore --configuration Release --output /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# The non-root user of the .NET images. The API listens on port 8080 (ASPNETCORE_HTTP_PORTS).
USER $APP_UID
EXPOSE 8080

COPY --from=build /app .
ENTRYPOINT ["dotnet", "Starter.Template.Api.dll"]
