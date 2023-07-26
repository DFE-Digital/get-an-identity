using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace TeacherIdentity.AuthServer;

public static class ExceptionExtensions
{
    public static bool IsUniqueIndexViolation(this DbUpdateException ex, string indexName) =>
        ex.InnerException is PostgresException pgException &&
            pgException.SqlState == PostgresErrorCodes.UniqueViolation &&
            pgException.ConstraintName == indexName;
}
