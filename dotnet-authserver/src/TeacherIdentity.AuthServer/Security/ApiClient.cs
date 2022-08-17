using System.Diagnostics.CodeAnalysis;

namespace TeacherIdentity.AuthServer.Security;

public class ApiClient
{
    [DisallowNull]
    public string? ClientId { get; set; }
    [DisallowNull]
    public string[]? ApiKeys { get; set; }
}
