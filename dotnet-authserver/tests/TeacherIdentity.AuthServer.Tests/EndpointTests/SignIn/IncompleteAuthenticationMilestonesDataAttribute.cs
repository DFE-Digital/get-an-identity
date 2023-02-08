using System.Reflection;
using Xunit.Sdk;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn;

public class IncompleteAuthenticationMilestonesDataAttribute : DataAttribute
{
    public IncompleteAuthenticationMilestonesDataAttribute(params AuthenticationState.AuthenticationMilestone[] exclude)
    {
        Exclude = exclude;
    }

    public AuthenticationState.AuthenticationMilestone[] Exclude { get; }

    public override IEnumerable<object[]> GetData(MethodInfo testMethod)
    {
        var milestones = Enum.GetValues<AuthenticationState.AuthenticationMilestone>()
            .Except(Exclude)
            .Except(new[] { AuthenticationState.AuthenticationMilestone.Complete });

        return milestones.Select(m => new object[] { m });
    }
}
