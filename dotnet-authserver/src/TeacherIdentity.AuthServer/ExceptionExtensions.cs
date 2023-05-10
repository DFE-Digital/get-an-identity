using EntityFramework.Exceptions.Common;
using Npgsql;

namespace TeacherIdentity.AuthServer;

public static class ExceptionExtensions
{
    public static bool IsUniqueIndexViolation(this UniqueConstraintException ex, string indexName) =>
        ex.InnerException is PostgresException pgException &&
            pgException.ConstraintName == indexName;
}
