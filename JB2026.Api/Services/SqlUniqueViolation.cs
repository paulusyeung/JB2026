using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Services;

public static class SqlUniqueViolation
{
    public static bool IsUniqueIndexViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException sql && sql.Number is 2601 or 2627)
            {
                return true;
            }
        }

        return false;
    }
}