using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TeacherIdentity.AuthServer.Models;

public class TeacherIdentityServerDesignTimeDbContextFactory : IDesignTimeDbContextFactory<TeacherIdentityServerDbContext>
{
    public TeacherIdentityServerDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<TeacherIdentityServerDesignTimeDbContextFactory>(optional: true)  // Optional for CI
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection") ??
            throw new Exception("Connection string DefaultConnection is missing.");

        var optionsBuilder = new DbContextOptionsBuilder<TeacherIdentityServerDbContext>();
        TeacherIdentityServerDbContext.ConfigureOptions(optionsBuilder, connectionString);

        return new TeacherIdentityServerDbContext(optionsBuilder.Options);
    }
}
