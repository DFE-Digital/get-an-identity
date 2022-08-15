using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TeacherIdentity.AuthServer;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services;

namespace TeacherIdentity.AuthServer.Tests.Services;

public class EmailConfirmationServiceTests : IClassFixture<DbFixture>
{
    private static readonly TimeSpan _pinLifetime = TimeSpan.FromSeconds(30);

    private readonly DbFixture _dbFixture;

    public EmailConfirmationServiceTests(DbFixture dbFixture)
    {
        _dbFixture = dbFixture;
    }

    [Fact]
    public async Task GeneratePin()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var clock = new TestClock();
        var service = CreateEmailConfirmationService(dbContext, clock);

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
                Assert.Equal(clock.UtcNow + _pinLifetime, p.Expires);
            });
    }

    [Fact]
    public async Task VerifyPin_UnknownPin_ReturnsFalse()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var clock = new TestClock();
        var service = CreateEmailConfirmationService(dbContext, clock);

        var email = Faker.Internet.Email();

        // The real PIN generation method never generates pins that start with a '0'
        var pin = "012345";

        // Act
        var result = await service.VerifyPin(email, pin);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task VerifyPin_ExpiredPin_ReturnsFalse()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var clock = new TestClock();
        var service = CreateEmailConfirmationService(dbContext, clock);

        var email = Faker.Internet.Email();
        var pin = await service.GeneratePin(email);

        clock.AdvanceBy(_pinLifetime);

        // Act
        var result = await service.VerifyPin(email, pin);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task VerifyPin_PinForDifferentEmailAddress_ReturnsFalse()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var clock = new TestClock();
        var service = CreateEmailConfirmationService(dbContext, clock);

        var email = Faker.Internet.Email();
        var anotherEmail = Faker.Internet.Email();
        var pin = await service.GeneratePin(anotherEmail);

        // Act
        var result = await service.VerifyPin(email, pin);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task VerifyPin_PinIsNotActive_ReturnsFalse()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var clock = new TestClock();
        var service = CreateEmailConfirmationService(dbContext, clock);

        var email = Faker.Internet.Email();
        var pin = await service.GeneratePin(email);

        var ecp = await dbContext.EmailConfirmationPins.SingleAsync(p => p.Email == email && p.Pin == pin);
        ecp.IsActive = false;
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.VerifyPin(email, pin);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task VerifyPin_ValidPin_ReturnsTrue()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var clock = new TestClock();
        var service = CreateEmailConfirmationService(dbContext, clock);

        var email = Faker.Internet.Email();
        var pin = await service.GeneratePin(email);

        // Act
        var result = await service.VerifyPin(email, pin);

        // Assert
        Assert.True(result);
    }

    private EmailConfirmationService CreateEmailConfirmationService(TeacherIdentityServerDbContext dbContext, IClock clock)
    {
        var configuration = new ConfigurationManager();
        configuration["EmailConfirmationPinLifetimeSeconds"] = _pinLifetime.TotalSeconds.ToString();

        var logger = NullLogger<EmailConfirmationService>.Instance;

        return new EmailConfirmationService(dbContext, clock, configuration, logger);
    }
}
