using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace TeacherIdentity.AuthServer.Tests;

public class ClaimTypeAndValueEqualityComparer : IEqualityComparer<Claim>
{
    public bool Equals(Claim? x, Claim? y)
    {
        return x is null && y is null ||
            (x is not null && y is not null && x.Type == y.Type && x.Value == y.Value);
    }

    public int GetHashCode([DisallowNull] Claim obj) => HashCode.Combine(obj.Type, obj.Value);
}
