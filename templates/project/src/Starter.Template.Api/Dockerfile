# L'image de l'API. Le contexte de construction est le dossier de la solution, pour Directory.Build.props et Directory.Packages.props :
#   docker build -f src/Starter.Template.Api/Dockerfile -t starter-template-api .
# ou avec compose, depuis le dossier de la solution : docker compose up --build api

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Le fichier projet d'abord : la couche de restauration reste en cache tant qu'aucune dépendance ne change.
COPY Directory.Build.props Directory.Packages.props global.json .editorconfig ./
COPY src/Starter.Template.Api/Starter.Template.Api.csproj src/Starter.Template.Api/
RUN dotnet restore src/Starter.Template.Api/Starter.Template.Api.csproj

COPY src/ src/
RUN dotnet publish src/Starter.Template.Api/Starter.Template.Api.csproj --no-restore --configuration Release --output /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# L'utilisateur non root des images .NET. L'API écoute sur le port 8080 (ASPNETCORE_HTTP_PORTS).
USER $APP_UID
EXPOSE 8080

COPY --from=build /app .
ENTRYPOINT ["dotnet", "Starter.Template.Api.dll"]
