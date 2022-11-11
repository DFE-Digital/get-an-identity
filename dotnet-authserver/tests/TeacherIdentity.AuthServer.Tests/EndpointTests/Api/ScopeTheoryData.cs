using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Api;

public static class ScopeTheoryData
{
    public static string First(this TheoryData<string> data) => (string)Enumerable.First(data)[0];

    public static TheoryData<string> FromScopes(params string[] scopes) => FromScopes((IEnumerable<string>)scopes);

    public static TheoryData<string> GetStaffUserScopes() =>
        FromScopes(CustomScopes.StaffUserTypeScopes);

    public static TheoryData<string> GetAllStaffUserScopesExcept(TheoryData<string> scopes) =>
        FromScopes(CustomScopes.StaffUserTypeScopes.Except(scopes.Select(d => (string)d.Single())).Append(""));

    public static TheoryData<string> GetAllStaffUserScopesExcept(IEnumerable<string> scopes) =>
        FromScopes(CustomScopes.StaffUserTypeScopes.Except(scopes));

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
