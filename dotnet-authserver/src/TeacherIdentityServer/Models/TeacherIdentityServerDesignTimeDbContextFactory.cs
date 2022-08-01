using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TeacherIdentityServer.Models;

public class TeacherIdentityServerDesignTimeDbContextFactory : IDesignTimeDbContextFactory<TeacherIdentityServerDbContext>
{
    public TeacherIdentityServerDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<TeacherIdentityServerDesignTimeDbContextFactory>(optional: true)  // Optional for CI
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<TeacherIdentityServerDbContext>();
        TeacherIdentityServerDbContext.ConfigureOptions(optionsBuilder, connectionString);

        return new TeacherIdentityServerDbContext(optionsBuilder.Options);
    }
}
