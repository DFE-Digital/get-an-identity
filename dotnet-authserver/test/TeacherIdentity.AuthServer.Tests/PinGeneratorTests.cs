using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace TeacherIdentity.AuthServer.Tests;

public class PinGeneratorTests : IClassFixture<DbFixture>
{
    private readonly DbFixture _dbFixture;

    public PinGeneratorTests(DbFixture dbFixture)
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

        var logger = NullLogger<PinGenerator>.Instance;

        var pinGenerator = new PinGenerator(dbContext, clock, configuration, logger);

        var email = Faker.Internet.Email();

        // Act
        var pin = await pinGenerator.GenerateEmailConfirmationPin(email);

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
