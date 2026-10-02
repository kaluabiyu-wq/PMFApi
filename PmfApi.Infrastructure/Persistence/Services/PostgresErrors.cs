

using Npgsql;

namespace PmfApi.Infrastructure.Persistence.Services;

internal static class PostgresErrors
{
    public static bool IsForeignKeyViolation(Exception ex) => HasSqlState(ex, PostgresErrorCodes.ForeignKeyViolation);

    public static bool IsUniqueViolation(Exception ex) => HasSqlState(ex, PostgresErrorCodes.UniqueViolation);

    private static bool HasSqlState(Exception ex, string sqlState)
    {
        Exception? current = ex;
        while (current is not null)
        {
            if (current is PostgresException pg && pg.SqlState == sqlState)
                return true;

            current = current.InnerException;
        }

        return false;
    }

}