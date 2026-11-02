using Microsoft.AspNetCore.Diagnostics;
#if (UseDatabase)
using Microsoft.Data.SqlClient;
#else
using Microsoft.Data.Sqlite;
#endif
using Microsoft.EntityFrameworkCore;

namespace Starter.Template.Api.Common.Data;

// Transforme une violation d'index unique en réponse problem details 409 Conflict. Seule la base peut garantir
// l'unicité : deux requêtes peuvent passer une vérification faite avant, et une seule insertion gagne.
// Toute autre exception passe au gestionnaire par défaut (UseExceptionHandler dans Program.cs) : une réponse
// problem details 500, avec l'exception seulement dans les logs.
internal sealed class UniqueConstraintExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not DbUpdateException { InnerException: var inner } || !IsUniqueViolation(inner))
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Status = StatusCodes.Status409Conflict,
                Detail = "Another resource already has this value, which must be unique.",
            },
        });
    }

#if (UseDatabase)
    // 2601 : clé en double dans un index unique, 2627 : dans une contrainte unique ou une clé primaire.
    private static bool IsUniqueViolation(Exception? exception) =>
        exception is SqlException { Number: 2601 or 2627 };
#else
    // SQLITE_CONSTRAINT_UNIQUE (2067) et SQLITE_CONSTRAINT_PRIMARYKEY (1555).
    private static bool IsUniqueViolation(Exception? exception) =>
        exception is SqliteException { SqliteExtendedErrorCode: 2067 or 1555 };
#endif
}
