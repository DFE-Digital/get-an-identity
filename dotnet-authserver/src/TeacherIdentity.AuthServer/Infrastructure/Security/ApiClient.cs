using System.Diagnostics.CodeAnalysis;

namespace TeacherIdentity.AuthServer.Infrastructure.Security;

public class ApiClient
{
    [DisallowNull]
    public string? ClientId { get; set; }
    [DisallowNull]
    public string[]? ApiKeys { get; set; }
}
