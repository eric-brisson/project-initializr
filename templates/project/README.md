# Starter.Template

Une minimal API .NET 10 organisée en tranches verticales, générée avec project-initializr.

## En bref

Les liens répondent une fois l'API lancée avec `dotnet run` (profil `http`).

| | |
| --- | --- |
| API | <http://localhost:5154/api/v1/about> |
<!--#if (UseScalar) -->
| Scalar | <http://localhost:5154/scalar> |
<!--#endif -->
<!--#if (UseSwaggerUi) -->
| Swagger UI | <http://localhost:5154/swagger> |
<!--#endif -->
<!--#if (UseScalar || UseSwaggerUi) -->
| Document OpenAPI | <http://localhost:5154/openapi/v1.json> |
<!--#else -->
| Documentation de l'API | Aucune (ni Scalar ni Swagger UI) |
<!--#endif -->
| Santé | <http://localhost:5154/actuator/health/readiness> |
<!--#if (UseTelemetry) -->
| Télémétrie (tableau de bord Aspire) | <http://localhost:18888> |
<!--#else -->
| Télémétrie | Non sélectionnée |
<!--#endif -->
<!--#if (UseDatabase) -->
| Base de données | SQL Server `localhost,1433`, base `StarterTemplate`, utilisateur `sa`, mot de passe dans `.env` |
<!--#elseif (UseInMemory) -->
| Base de données | SQLite en mémoire, vidée à chaque arrêt de l'API |
<!--#else -->
| Base de données | Aucune : un store en mémoire par feature |
<!--#endif -->
| Dans un conteneur | <http://localhost:8080/api/v1/about> (`docker compose up --build`) |

## Prérequis

- Le SDK .NET 10.
<!--#if (UseDatabase) -->
- Docker, pour SQL Server et les tests d'intégration.
<!--#elseif (UseTelemetry) -->
- Docker, pour le tableau de bord de télémétrie et pour exécuter l'API dans un conteneur.
<!--#else -->
- Docker, seulement pour exécuter l'API dans un conteneur.
<!--#endif -->

## Lancer l'API

```bash
dotnet run --project src/Starter.Template.Api
```

<!--#if (UseScalar || UseSwaggerUi) -->
En Development, la documentation de l'API est servie sur :

<!--#if (UseScalar) -->
- `/scalar` pour la référence d'API Scalar.
<!--#endif -->
<!--#if (UseSwaggerUi) -->
- `/swagger` pour Swagger UI.
<!--#endif -->
- `/openapi/v1.json` pour le document OpenAPI.

<!--#endif -->
`src/Starter.Template.Api/Starter.Template.Api.http` contient des requêtes prêtes à l'emploi pour l'extension
REST Client de VS Code et les clients HTTP de Visual Studio ou de Rider.

## Tester

```bash
dotnet test
```

<!--#if (UseDatabase) -->
Les tests d'intégration démarrent un SQL Server jetable avec Testcontainers et le migrent avec Flyway.
Sans Docker, ils sont ignorés, sauf quand la variable `CI` est définie, comme sur GitHub Actions : ils échouent alors.

<!--#endif -->
## Points de terminaison

| Route | Rôle |
| --- | --- |
| `GET /api/v1/about` | Le nom et la version de l'API, et les principaux composants qui la constituent |
| `/api/v1/technologies` | La feature d'exemple : liste, lecture, création, modification et suppression |
| `GET /actuator/health/liveness` | Sonde de vivacité : le processus répond |
| `GET /actuator/health/readiness` | Sonde de disponibilité : toutes les vérifications marquées `ready` passent |

Les erreurs suivent le format problem details de la RFC 9457 : échecs de validation (400), identifiants inconnus (404),
noms en double (409) et exceptions inattendues (500, avec les détails uniquement dans les logs).

<!--#if (UseDatabase) -->
## Base de données

SQL Server tourne dans Docker, et Flyway applique les scripts de `db/migrations`. EF Core lit et écrit les
données, mais ne crée pas le schéma : une nouvelle table ou colonne va dans un nouveau script, `V<n>__<description>.sql`.
Ne modifiez jamais un script déjà appliqué.

```bash
docker compose up -d sqlserver        # démarrer SQL Server
docker compose run --rm flyway        # appliquer les migrations
```

Le mot de passe sa est dans `.env` (non versionné), pour docker compose. La chaîne de connexion complète est dans
les secrets utilisateur de l'API, qui remplacent `appsettings.Development.json` en Development :

```bash
dotnet user-secrets list --project src/Starter.Template.Api
```

<!--#endif -->
<!--#if (UseTelemetry) -->
## Télémétrie locale

Le tableau de bord Aspire affiche les traces, les métriques et les logs de l'API pendant le développement : chaque
requête en cascade de ses spans (appels HTTP, requêtes SQL, spans personnalisés), avec les logs écrits pendant celle-ci.

Lancez l'API depuis l'IDE (F5) ou avec `dotnet run`, puis ouvrez <http://localhost:18888>. Quand rien n'écoute
encore la télémétrie, l'API démarre elle-même le tableau de bord dans Docker (`Common/Telemetry/DashboardLauncher.cs`),
en arrière-plan. La première fois, le téléchargement de l'image prend une minute. Sans Docker, l'API écrit un
avertissement dans les logs et fonctionne quand même. Pour démarrer le tableau de bord à la main :

```bash
docker compose up -d dashboard
```

Les profils de lancement définissent `OTEL_EXPORTER_OTLP_ENDPOINT` vers le tableau de bord, ce qui active le SDK
OpenTelemetry (`Common/Telemetry/TelemetryRegistration.cs`), et `Telemetry__StartDashboard`, qui permet à l'API de le
démarrer. Sans elles, comme dans les tests ou une fois déployée, rien n'est enregistré : dans ARO, Dynatrace OneAgent
instrumente l'API de lui-même. Le tableau de bord garde les données en mémoire seulement. Un seul tableau de bord sert
tous les projets : quand celui d'un autre projet écoute déjà sur le port 4317, l'API y envoie sa télémétrie.

Les requêtes HTTP entrantes et sortantes et les requêtes SQL sont tracées automatiquement : aucune slice n'a de code
de télémétrie. `AppTelemetry` (du `System.Diagnostics` standard) fournit une source et un compteur pour les cas où un
span ou une métrique personnalisés valent la peine : un traitement que rien ne trace (tâche de fond, consommateur de
file), une étape lente dans une requête (génération de PDF, calcul), un identifiant métier pour retrouver une trace,
ou un chiffre métier qu'aucune métrique HTTP ne donne. Son commentaire montre comment faire.

<!--#endif -->
<!--#if (UseInMemory) -->
## Base de données

EF Core stocke les données dans une base SQLite en mémoire, créée à partir du modèle EF Core au démarrage de l'API
et vidée à son arrêt. Docker n'est pas nécessaire.

<!--#endif -->
## Exécuter dans un conteneur

```bash
docker compose up --build
```

L'API répond sur <http://localhost:8080> avec la configuration de Production, donc sans la documentation de l'API.
Son image est construite à partir de `src/Starter.Template.Api/Dockerfile`.

## Structure du projet

```
src/Starter.Template.Api/
  Program.cs                L'hôte : services, middlewares, sondes de santé
  Common/                   Ce que les features partagent : enregistrement des routes et des services
<!--#if (UseEfCore) -->
  Common/Data/              AppDbContext et la configuration de la base de données
<!--#endif -->
<!--#if (UseTelemetry) -->
  Common/Telemetry/         La configuration d'OpenTelemetry, la source et le compteur de la télémétrie personnalisée
<!--#endif -->
  Features/<Feature>/       Un dossier par feature, un fichier par point de terminaison (une tranche verticale)
tests/
  Starter.Template.Api.UnitTests/
  Starter.Template.Api.IntegrationTests/   L'API en mémoire (WebApplicationFactory)
<!--#if (UseDatabase) -->
db/migrations/              Les scripts Flyway
<!--#endif -->
```

Une tranche, c'est un fichier par point de terminaison : sa requête, son handler et sa route. Elle accède directement
aux données (EF Core ou le store de la feature), sans couche repository ni médiateur. À côté des tranches, une feature a :

- l'entité stockée (`Technology.cs`, avec son mapping EF Core) ;
- ce que l'API renvoie (`TechnologyDto.cs`, avec la méthode `From` qui fait la conversion) ;
- son point d'entrée (`TechnologiesFeature.cs`), qui enregistre ses services et ses routes, appelé depuis `Common/`.

## Ajouter une feature

Depuis le dossier de la solution, avec l'outil starter-init :

```bash
starter-init feature Account
```

Ou directement avec le modèle :

```bash
<!--#if (UseDatabase) -->
dotnet new starter-feature -n Account --plural Accounts --solution Starter.Template --data sqlserver
<!--#elseif (UseInMemory) -->
dotnet new starter-feature -n Account --plural Accounts --solution Starter.Template --data sqlite
<!--#else -->
dotnet new starter-feature -n Account --plural Accounts --solution Starter.Template --data none
<!--#endif -->
```

Elle génère les mêmes cinq tranches et les mêmes tests que la feature Technologies. Branchez-la ensuite dans
`Common/EndpointRegistration.cs` et `Common/ServiceRegistration.cs`,
<!--#if (UseDatabase) -->
créez sa table dans un nouveau script Flyway,
<!--#endif -->
puis complétez les commentaires `TODO`.

## Conventions

- Les versions des paquets sont définies une seule fois, dans `Directory.Packages.props`.
- `Directory.Build.props` définit le framework cible et l'analyse de code pour tous les projets.
  Les avertissements font échouer la compilation, et `.editorconfig` contient les règles de style vérifiées par la compilation.
- `.starter.json` indique à starter-init le module de données utilisé par la solution.
