using StackExchange.Redis;

namespace TeacherIdentity.AuthServer.Infrastructure.Redis;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRedis(
        this IServiceCollection services,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        IHealthChecksBuilder healthChecksBuilder)
    {
        if (environment.IsProduction())
        {
            var connectionString = configuration.GetConnectionString("Redis") ??
                throw new Exception("Missing Redis connection string.");

            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(connectionString));
            services.AddStackExchangeRedisCache(options => options.Configuration = connectionString);

            healthChecksBuilder.AddRedis(connectionString);
        }

        return services;
    }
}
