using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TeacherIdentity.AuthServer;
using TeacherIdentity.AuthServer.Services;

namespace TeacherIdentity.AuthServer.Tests.Services;

public class EmailConfirmationServiceTests : IClassFixture<DbFixture>
{
    private readonly DbFixture _dbFixture;

    public EmailConfirmationServiceTests(DbFixture dbFixture)
    {
        _dbFixture = dbFixture;
    }

    [Fact]
    public async Task GenerateEmailConfirmationPin_GeneratesAndPersistsPin()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var clock = new TestClock();

        var lifetime = TimeSpan.FromSeconds(30);
        var configuration = new ConfigurationManager();
        configuration["EmailConfirmationPinLifetimeSeconds"] = lifetime.TotalSeconds.ToString();

        var logger = NullLogger<EmailConfirmationService>.Instance;

        var service = new EmailConfirmationService(dbContext, clock, configuration, logger);

        var email = Faker.Internet.Email();

        // Act
        var pin = await service.GeneratePin(email);

        // Assert
        var emailConfirmationPins = await dbContext.EmailConfirmationPins.Where(p => p.Email == email).ToListAsync();

        Assert.Collection(
            emailConfirmationPins,
            p =>
            {
                Assert.True(p.IsActive);
                Assert.Equal(email, p.Email);
                Assert.Equal(pin, p.Pin);
                Assert.Equal(clock.UtcNow + lifetime, p.Expires);
            });
    }
}
