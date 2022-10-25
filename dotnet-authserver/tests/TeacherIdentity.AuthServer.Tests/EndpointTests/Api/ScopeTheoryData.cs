using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Api;

public static class ScopeTheoryData
{
    public static string First(this TheoryData<string> data) => (string)Enumerable.First(data)[0];

    public static TheoryData<string> GetAdminScopes() =>
        FromScopes(CustomScopes.AdminScopes);

    public static TheoryData<string> GetAllAdminScopesExcept(TheoryData<string> scopes) =>
        FromScopes(CustomScopes.AdminScopes.Except(scopes.Select(d => (string)d.Single())).Append(""));

    public static TheoryData<string> GetAllAdminScopesExcept(IEnumerable<string> scopes) =>
        FromScopes(CustomScopes.AdminScopes.Except(scopes));

    public static TheoryData<string> Single(string scope) => FromScopes(new[] { scope });

    private static TheoryData<string> FromScopes(IEnumerable<string> scopes)
    {
        var data = new TheoryData<string>();

        foreach (var scope in scopes)
        {
            data.Add(scope);
        }

        return data;
    }
}
