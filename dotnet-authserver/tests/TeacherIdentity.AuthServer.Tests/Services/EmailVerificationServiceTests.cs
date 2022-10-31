using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services.Email;
using TeacherIdentity.AuthServer.Services.EmailVerification;
using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests.Services;

[Collection(nameof(DisableParallelization))]  // Changes the clock
public class EmailVerificationServiceTests : IClassFixture<DbFixture>
{
    private static readonly TimeSpan _pinLifetime = TimeSpan.FromSeconds(120);

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
        var emailSenderMock = new Mock<IEmailSender>();
        var clock = new TestClock();
        var currentClientProviderMock = new Mock<ICurrentClientProvider>();
        var rateLimiter = new Mock<IRateLimitStore>();
        var requestClientIpProvider = new TestRequestClientIpProvider();
        var service = CreateEmailConfirmationService(dbContext, emailSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

        var currentClientDisplayName = "Test app";
        currentClientProviderMock.Setup(mock => mock.GetCurrentClient()).ReturnsAsync(new Application() { DisplayName = currentClientDisplayName });

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
        var pinResult = await service.GeneratePin(email);

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
                Assert.Equal(pinResult.Pin, newPin.Pin);
                Assert.Equal(clock.UtcNow + _pinLifetime, newPin.Expires);
            });

        var expectedEmailSubject = "Confirm your email address";

        var expectedEmailBody = $"Use this code to confirm your email address:\n\n" +
            $"{pinResult.Pin}\n\n" +
            $"The code will expire after 2 minutes.\n\n" +
            $"This email address has been used for {currentClientDisplayName}.\n\n" +
            $"If this was not you, you can ignore this email.\n\nDepartment for Education";

        emailSenderMock.Verify(mock => mock.SendEmail(email, expectedEmailSubject, expectedEmailBody), Times.Once());
    }

    [Fact]
    public async Task VerifyPin_UnknownPin_ReturnsFalse()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var emailSenderMock = new Mock<IEmailSender>();
        var clock = new TestClock();
        var currentClientProviderMock = new Mock<ICurrentClientProvider>();
        var rateLimiter = new Mock<IRateLimitStore>();
        var requestClientIpProvider = new TestRequestClientIpProvider();
        var service = CreateEmailConfirmationService(dbContext, emailSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

        var email = Faker.Internet.Email();

        // The real PIN generation method never generates pins that start with a '0'
        var pin = "012345";

        // Act
        var result = await service.VerifyPin(email, pin);

        // Assert
        Assert.Equal(PinVerificationFailedReasons.Unknown, result);
    }

    [Fact]
    public async Task VerifyPin_ExceededMaxFailureAttempts_ReturnsRateLimitExceeded()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var emailSenderMock = new Mock<IEmailSender>();
        var clock = new TestClock();
        var currentClientProviderMock = new Mock<ICurrentClientProvider>();
        var rateLimiter = new Mock<IRateLimitStore>();
        rateLimiter.Setup(x => x.IsClientIpBlockedForPinVerification(It.IsAny<string>())).Returns(Task.FromResult(true));
        var requestClientIpProvider = new TestRequestClientIpProvider();
        var service = CreateEmailConfirmationService(dbContext, emailSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

        var email = Faker.Internet.Email();

        // The real PIN generation method never generates pins that start with a '0'
        var pin = "012345";

        // Act
        var result = await service.VerifyPin(email, pin);

        // Assert
        Assert.Equal(PinVerificationFailedReasons.RateLimitExceeded, result);
    }

    [Fact]
    public async Task VerifyPin_ExpiredPin_ReturnsFalse()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var emailSenderMock = new Mock<IEmailSender>();
        var clock = new TestClock();
        var currentClientProviderMock = new Mock<ICurrentClientProvider>();
        var rateLimiter = new Mock<IRateLimitStore>();
        var requestClientIpProvider = new TestRequestClientIpProvider();
        var service = CreateEmailConfirmationService(dbContext, emailSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

        var email = Faker.Internet.Email();
        var pinResult = await service.GeneratePin(email);

        clock.AdvanceBy(_pinLifetime);

        // Act
        var result = await service.VerifyPin(email, pinResult.Pin!);

        // Assert
        Assert.True(result.HasFlag(PinVerificationFailedReasons.Expired));
    }

    [Fact]
    public async Task VerifyPin_PinForDifferentEmailAddress_ReturnsFalse()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var emailSenderMock = new Mock<IEmailSender>();
        var clock = new TestClock();
        var currentClientProviderMock = new Mock<ICurrentClientProvider>();
        var rateLimiter = new Mock<IRateLimitStore>();
        var requestClientIpProvider = new TestRequestClientIpProvider();
        var service = CreateEmailConfirmationService(dbContext, emailSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

        var email = Faker.Internet.Email();
        var anotherEmail = Faker.Internet.Email();
        var pinResult = await service.GeneratePin(anotherEmail);

        // Act
        var result = await service.VerifyPin(email, pinResult.Pin!);

        // Assert
        Assert.Equal(PinVerificationFailedReasons.Unknown, result);
    }

    [Fact]
    public async Task VerifyPin_PinIsNotActive_ReturnsFalse()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var emailSenderMock = new Mock<IEmailSender>();
        var clock = new TestClock();
        var currentClientProviderMock = new Mock<ICurrentClientProvider>();
        var rateLimiter = new Mock<IRateLimitStore>();
        var requestClientIpProvider = new TestRequestClientIpProvider();
        var service = CreateEmailConfirmationService(dbContext, emailSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

        var email = Faker.Internet.Email();
        var pinResult = await service.GeneratePin(email);

        var ecp = await dbContext.EmailConfirmationPins.SingleAsync(p => p.Email == email && p.Pin == pinResult.Pin);
        ecp.IsActive = false;
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.VerifyPin(email, pinResult.Pin!);

        // Assert
        Assert.Equal(PinVerificationFailedReasons.NotActive, result);
    }

    [Fact]
    public async Task VerifyPin_ValidPin_DeactivatesPinAndReturnsTrue()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var emailSenderMock = new Mock<IEmailSender>();
        var clock = new TestClock();
        var currentClientProviderMock = new Mock<ICurrentClientProvider>();
        var rateLimiter = new Mock<IRateLimitStore>();
        var rateLimiterOptions = Options.Create(new RateLimitStoreOptions()
        {
            PinVerificationMaxFailures = 5,
            PinVerificationFailureTimeoutSeconds = 120,
            PinGenerationMaxFailures = 5,
            PinGenerationTimeoutSeconds = 120
        });
        var requestClientIpProvider = new TestRequestClientIpProvider();
        var service = CreateEmailConfirmationService(dbContext, emailSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

        var email = Faker.Internet.Email();
        var pinResult = await service.GeneratePin(email);

        // Act
        var result = await service.VerifyPin(email, pinResult.Pin!);

        // Assert
        Assert.Equal(PinVerificationFailedReasons.None, result);

        var emailConfirmationPin = await dbContext.EmailConfirmationPins.Where(p => p.Email == email && p.Pin == pinResult.Pin).SingleOrDefaultAsync();
        Assert.False(emailConfirmationPin?.IsActive);
        Assert.Equal(clock.UtcNow, emailConfirmationPin?.VerifiedOn);
    }

    private EmailVerificationService CreateEmailConfirmationService(
        TeacherIdentityServerDbContext dbContext,
        IEmailSender emailSender,
        IClock clock,
        ICurrentClientProvider currentClientProvider,
        IRateLimitStore rateLimitStore,
        IRequestClientIpProvider clientIpProvider)
    {
        var options = Options.Create(new EmailVerificationOptions()
        {
            PinLifetimeSeconds = (int)_pinLifetime.TotalSeconds
        });

        var logger = NullLogger<EmailVerificationService>.Instance;

        return new EmailVerificationService(dbContext, emailSender, clock, currentClientProvider, options, logger, rateLimitStore, clientIpProvider);
    }
}
