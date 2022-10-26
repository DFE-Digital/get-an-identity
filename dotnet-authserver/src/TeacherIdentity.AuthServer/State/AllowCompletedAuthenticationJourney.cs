using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace TeacherIdentity.AuthServer.State;

/// <summary>
/// Annotates an endpoint to indicate that requests for an already-completed journey are allowed.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class AllowCompletedAuthenticationJourneyAttribute : Attribute, IPageApplicationModelConvention
{
    public void Apply(PageApplicationModel model)
    {
        model.EndpointMetadata.Add(AllowCompletedAuthenticationJourneyMarker.Instance);
    }
}

public sealed class AllowCompletedAuthenticationJourneyMarker
{
    private AllowCompletedAuthenticationJourneyMarker()
    {
    }

    public static AllowCompletedAuthenticationJourneyMarker Instance { get; } = new();
}
