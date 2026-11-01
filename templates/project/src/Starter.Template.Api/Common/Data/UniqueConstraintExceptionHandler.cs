using Microsoft.AspNetCore.Diagnostics;
#if (UseDatabase)
using Microsoft.Data.SqlClient;
#else
using Microsoft.Data.Sqlite;
#endif
using Microsoft.EntityFrameworkCore;

namespace Starter.Template.Api.Common.Data;

// Turns a unique index violation into a 409 Conflict problem details response. The database is the only place
// that can enforce uniqueness: two requests can both pass a check made beforehand, and only one insert wins.
// Any other exception goes on to the default handler (UseExceptionHandler in Program.cs): a 500 problem details
// response, with the exception in the logs only.
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
    // 2601: duplicate key in a unique index, 2627: in a unique constraint or a primary key.
    private static bool IsUniqueViolation(Exception? exception) =>
        exception is SqlException { Number: 2601 or 2627 };
#else
    // SQLITE_CONSTRAINT_UNIQUE (2067) and SQLITE_CONSTRAINT_PRIMARYKEY (1555).
    private static bool IsUniqueViolation(Exception? exception) =>
        exception is SqliteException { SqliteExtendedErrorCode: 2067 or 1555 };
#endif
}
