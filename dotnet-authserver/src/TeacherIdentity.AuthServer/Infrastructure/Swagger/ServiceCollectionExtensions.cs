using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TeacherIdentity.AuthServer.Infrastructure.Swagger;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo() { Title = "Get an identity to access Teacher Services API", Version = "v1" });

            c.AddSecurityDefinition(
                "oauth2",
                new OpenApiSecurityScheme()
                {
                    In = ParameterLocation.Header,
                    Scheme = "Bearer",
                    Type = SecuritySchemeType.OpenIdConnect,
                    OpenIdConnectUrl = new Uri("/.well-known/openid-configuration", UriKind.Relative)
                });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement()
            {
                [
                    new OpenApiSecurityScheme()
                    {
                        Reference = new OpenApiReference()
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "oauth2"
                        }
                    }
                ] = Array.Empty<string>()
            });

            c.DocInclusionPredicate((docName, api) => docName.Equals(api.GroupName, StringComparison.OrdinalIgnoreCase));
            c.EnableAnnotations();
            c.ExampleFilters();
            c.OperationFilter<ResponseContentTypeOperationFilter>();
            c.OperationFilter<RateLimitOperationFilter>();
        });

        services.AddSwaggerExamplesFromAssemblyOf<Program>();

        services.AddTransient<ISerializerDataContractResolver>(sp =>
        {
            var serializerOptions = sp.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
            return new JsonSerializerDataContractResolver(serializerOptions);
        });

        return services;
    }
}
