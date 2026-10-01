

using Npgsql;

namespace PmfApi.Infrastructure.Persistence.Services;

internal static class PostgresErrors
{
    public static bool IsForeignKeyViolation(Exception ex)
    {
        Exception? current = ex;
        while (current is not null)
        {
            if(current is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation})
             return true;
             current = current.InnerException;
        }
        return false;
    }

}