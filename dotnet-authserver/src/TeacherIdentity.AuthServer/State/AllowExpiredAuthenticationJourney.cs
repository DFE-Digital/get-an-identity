using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace TeacherIdentity.AuthServer.State;

/// <summary>
/// Annotates an endpoint to indicate that requests for an expired journey are allowed.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class AllowExpiredAuthenticationJourneyAttribute : Attribute, IPageApplicationModelConvention
{
    public void Apply(PageApplicationModel model)
    {
        model.EndpointMetadata.Add(AllowExpiredAuthenticationJourneyMarker.Instance);
    }
}

public sealed class AllowExpiredAuthenticationJourneyMarker
{
    private AllowExpiredAuthenticationJourneyMarker()
    {
    }

    public static AllowExpiredAuthenticationJourneyMarker Instance { get; } = new();
}
