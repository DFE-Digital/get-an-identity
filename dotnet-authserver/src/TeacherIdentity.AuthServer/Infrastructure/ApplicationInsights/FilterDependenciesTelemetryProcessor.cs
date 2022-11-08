using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace TeacherIdentity.AuthServer.Infrastructure.ApplicationInsights;

public class FilterDependenciesTelemetryProcessor : ITelemetryProcessor
{
    private readonly ITelemetryProcessor _next;

    public FilterDependenciesTelemetryProcessor(ITelemetryProcessor next)
    {
        _next = next;
    }

    public void Process(ITelemetry item)
    {
        if (item is DependencyTelemetry dependency)
        {
            if (dependency.Target.EndsWith(".sentry.io") || dependency.Type == "Azure Service Bus")
            {
                return;
            }
        }

        _next.Process(item);
    }
}
