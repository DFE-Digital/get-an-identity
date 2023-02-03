namespace TeacherIdentity.AuthServer.Services.Csv;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCsv(
        this IServiceCollection services)
    {
        services.AddSingleton<IUserImportCsvStorageService, AzureUserImportCsvStorageService>();
        return services;
    }
}
