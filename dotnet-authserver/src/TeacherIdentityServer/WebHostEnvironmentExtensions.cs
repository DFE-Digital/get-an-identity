namespace TeacherIdentityServer;

public static class WebHostEnvironmentExtensions
{
    public static bool IsEndToEndTests(this IWebHostEnvironment environment) =>
        environment.EnvironmentName.Equals("EndToEndTests");
}
