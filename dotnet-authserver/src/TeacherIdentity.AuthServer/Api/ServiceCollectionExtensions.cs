using FluentValidation;
using MediatR;

namespace TeacherIdentity.AuthServer.Api;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddMediatR(typeof(Program));

        services.AddValidatorsFromAssemblyContaining(typeof(Program));

        services.AddScoped<ICurrentUserProvider, ClaimsPrincipalCurrentUserProvider>();

        return services;
    }
}
