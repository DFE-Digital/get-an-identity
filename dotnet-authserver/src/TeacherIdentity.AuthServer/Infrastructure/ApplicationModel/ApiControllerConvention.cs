using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using TeacherIdentity.AuthServer.Api.Filters;
using TeacherIdentity.AuthServer.Infrastructure.Filters;

namespace TeacherIdentity.AuthServer.Infrastructure.ApplicationModel;

public class ApiControllerConvention : IControllerModelConvention
{
    private static readonly Regex _versionedApiControllerNamespacePattern = new Regex(@"\.Api\.V(\d+)\.");

    public void Apply(ControllerModel controller)
    {
        var controllerNamespace = controller.ControllerType.Namespace!;
        var versionedApiControllerMatch = _versionedApiControllerNamespacePattern.Match(controllerNamespace);

        if (versionedApiControllerMatch.Success && int.TryParse(versionedApiControllerMatch.Groups[1].Value, out var version))
        {
            ApplyGroupName();
            ApplyRoutePrefix();
            ApplyFilters();

            // Group name is used to partition the operations by version into different swagger docs
            void ApplyGroupName() => controller.ApiExplorer.GroupName = $"v{version}";

            // A V1 operation gets a /api/v1 route prefix, V2 operation a /api/v2 route prefix etc.
            void ApplyRoutePrefix()
            {
                var routePrefix = new AttributeRouteModel(new RouteAttribute($"api/v{version}"));

                foreach (var selector in controller.Selectors)
                {
                    selector.AttributeRouteModel = selector.AttributeRouteModel != null ?
                        AttributeRouteModel.CombineAttributeRouteModel(routePrefix, selector.AttributeRouteModel) :
                        routePrefix;
                }
            }

            void ApplyFilters()
            {
                controller.Filters.Add(new ProducesJsonOrProblemAttribute());
                controller.Filters.Add(new DefaultErrorExceptionFilter(statusCode: StatusCodes.Status400BadRequest));
                controller.Filters.Add(new HandleValidationExceptionFilter());
            }
        }
    }
}
