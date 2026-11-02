using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Starter.Template.Api.Common.Telemetry;

// La source des spans personnalisés et le compteur des métriques personnalisées, nommés comme l'API. C'est du .NET
// standard (System.Diagnostics) : sans écouteur, ils ne coûtent presque rien. Le SDK OpenTelemetry (TelemetryRegistration)
// les écoute en local ; dans ARO, Dynatrace OneAgent le peut aussi, s'il est réglé pour capter les sources d'activité .NET.
//
// Aucune tranche ne les utilise : les requêtes HTTP, les appels HTTP et les requêtes SQL sont tracés automatiquement.
// Écrivez un span personnalisé pour un traitement que rien ne trace (tâche de fond, consommateur de file), pour une étape
// lente dans une requête (un PDF, un calcul), ou pour marquer une trace d'un identifiant métier, par exemple dans un handler :
//     using var activity = AppTelemetry.Source.StartActivity("ComputePrice");
//     activity?.SetTag("order.id", orderId);
// Et une métrique personnalisée pour un chiffre métier qu'aucune métrique HTTP ne donne (montants, articles par commande, taille d'une file).
public static class AppTelemetry
{
    public static readonly string Name = typeof(AppTelemetry).Assembly.GetName().Name!;

    public static readonly ActivitySource Source = new(Name);

    public static readonly Meter Meter = new(Name);
}
