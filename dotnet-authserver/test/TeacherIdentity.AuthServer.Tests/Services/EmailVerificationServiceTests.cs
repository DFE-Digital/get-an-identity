using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.Email;
using TeacherIdentity.AuthServer.Services.EmailVerification;

namespace TeacherIdentity.AuthServer.Tests.Services;

public class EmailVerificationServiceTests : IClassFixture<DbFixture>
{
    private static readonly TimeSpan _pinLifetime = TimeSpan.FromSeconds(30);

    private readonly DbFixture _dbFixture;

    public EmailVerificationServiceTests(DbFixture dbFixture)
    {
        _dbFixture = dbFixture;
    }

    [Fact]
    public async Task GeneratePin_AddsPinToDbDeactivatesOlderPinsForEmailAndEmailsPin()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var emailSender = A.Fake<IEmailSender>();
        var clock = new TestClock();
        var service = CreateEmailConfirmationService(dbContext, emailSender, clock);

        var email = Faker.Internet.Email();

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            dbContext.EmailConfirmationPins.Add(new EmailConfirmationPin()
            {
                Email = email,
                Expires = clock.UtcNow + _pinLifetime,
                IsActive = true,
                VerifiedOn = null,
                Pin = "01234"
            });

            await dbContext.SaveChangesAsync();
        });

        // Act
        var pin = await service.GeneratePin(email);

        // Assert
        var emailConfirmationPins = await dbContext.EmailConfirmationPins.Where(p => p.Email == email)
            .OrderBy(p => p.Expires)
            .ToListAsync();

        Assert.Collection(
            emailConfirmationPins,
            oldPin =>
            {
                Assert.False(oldPin.IsActive);
            },
            newPin =>
            {
                Assert.Null(newPin.VerifiedOn);
                Assert.Equal(email, newPin.Email);
                Assert.Equal(pin, newPin.Pin);
                Assert.Equal(clock.UtcNow + _pinLifetime, newPin.Expires);
            });

        A.CallTo(() => emailSender.SendEmailAddressConfirmationEmail(email, pin)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task VerifyPin_UnknownPin_ReturnsFalse()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var emailSender = A.Fake<IEmailSender>();
        var clock = new TestClock();
        var service = CreateEmailConfirmationService(dbContext, emailSender, clock);

        var email = Faker.Internet.Email();

        // The real PIN generation method never generates pins that start with a '0'
        var pin = "012345";

        // Act
        var result = await service.VerifyPin(email, pin);

        // Assert
        Assert.Equal(PinVerificationFailedReasons.Unknown, result);
    }

    [Fact]
    public async Task VerifyPin_ExpiredPin_ReturnsFalse()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var emailSender = A.Fake<IEmailSender>();
        var clock = new TestClock();
        var service = CreateEmailConfirmationService(dbContext, emailSender, clock);

        var email = Faker.Internet.Email();
        var pin = await service.GeneratePin(email);

        clock.AdvanceBy(_pinLifetime);

        // Act
        var result = await service.VerifyPin(email, pin);

        // Assert
        Assert.True(result.HasFlag(PinVerificationFailedReasons.Expired));
    }

    [Fact]
    public async Task VerifyPin_PinForDifferentEmailAddress_ReturnsFalse()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var emailSender = A.Fake<IEmailSender>();
        var clock = new TestClock();
        var service = CreateEmailConfirmationService(dbContext, emailSender, clock);

        var email = Faker.Internet.Email();
        var anotherEmail = Faker.Internet.Email();
        var pin = await service.GeneratePin(anotherEmail);

        // Act
        var result = await service.VerifyPin(email, pin);

        // Assert
        Assert.Equal(PinVerificationFailedReasons.Unknown, result);
    }

    [Fact]
    public async Task VerifyPin_PinIsNotActive_ReturnsFalse()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var emailSender = A.Fake<IEmailSender>();
        var clock = new TestClock();
        var service = CreateEmailConfirmationService(dbContext, emailSender, clock);

        var email = Faker.Internet.Email();
        var pin = await service.GeneratePin(email);

        var ecp = await dbContext.EmailConfirmationPins.SingleAsync(p => p.Email == email && p.Pin == pin);
        ecp.IsActive = false;
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.VerifyPin(email, pin);

        // Assert
        Assert.Equal(PinVerificationFailedReasons.NotActive, result);
    }

    [Fact]
    public async Task VerifyPin_ValidPin_DeactivatesPinAndReturnsTrue()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var emailSender = A.Fake<IEmailSender>();
        var clock = new TestClock();
        var service = CreateEmailConfirmationService(dbContext, emailSender, clock);

        var email = Faker.Internet.Email();
        var pin = await service.GeneratePin(email);

        // Act
        var result = await service.VerifyPin(email, pin);

        // Assert
        Assert.Equal(PinVerificationFailedReasons.None, result);

        var emailConfirmationPin = await dbContext.EmailConfirmationPins.Where(p => p.Email == email && p.Pin == pin).SingleOrDefaultAsync();
        Assert.False(emailConfirmationPin?.IsActive);
        Assert.Equal(clock.UtcNow, emailConfirmationPin?.VerifiedOn);
    }

    private EmailVerificationService CreateEmailConfirmationService(
        TeacherIdentityServerDbContext dbContext,
        IEmailSender emailSender,
        IClock clock)
    {
        var options = Options.Create(new EmailVerificationOptions()
        {
            PinLifetimeSeconds = (int)_pinLifetime.TotalSeconds
        });

        var logger = NullLogger<EmailVerificationService>.Instance;

        return new EmailVerificationService(dbContext, emailSender, clock, options, logger);
    }
}