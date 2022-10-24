using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TeacherIdentity.AuthServer.Infrastructure.Swagger;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo() { Title = "Get an identity to access Teacher Services API", Version = "v1" });

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
