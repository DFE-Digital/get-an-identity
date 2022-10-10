using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services.Email;
using TeacherIdentity.AuthServer.Services.EmailVerification;

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
        var httpAccesor = new Mock<IHttpContextAccessor>();
        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.Connection.RemoteIpAddress).Returns(new System.Net.IPAddress(123456));
        httpAccesor.Setup(x => x.HttpContext).Returns(httpContext.Object);
        var service = CreateEmailConfirmationService(dbContext, emailSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, httpAccesor.Object);

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

        var expectedEmailSubject = "Confirm your email address";

        var expectedEmailBody = $"Use this code to confirm your email address:\n\n" +
            $"{pin}\n\n" +
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
        var httpAccesor = new Mock<IHttpContextAccessor>();
        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.Connection.RemoteIpAddress).Returns(new System.Net.IPAddress(123456));
        httpAccesor.Setup(x => x.HttpContext).Returns(httpContext.Object);
        var service = CreateEmailConfirmationService(dbContext, emailSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, httpAccesor.Object);

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
        var httpAccesor = new Mock<IHttpContextAccessor>();
        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.Connection.RemoteIpAddress).Returns(new System.Net.IPAddress(123456));
        httpAccesor.Setup(x => x.HttpContext).Returns(httpContext.Object);
        rateLimiter.Setup(x => x.IsClientIpBlocked(It.IsAny<string>())).Returns(Task.FromResult(true));
        var service = CreateEmailConfirmationService(dbContext, emailSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, httpAccesor.Object);

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
        var httpAccesor = new Mock<IHttpContextAccessor>();
        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.Connection.RemoteIpAddress).Returns(new System.Net.IPAddress(123456));
        httpAccesor.Setup(x => x.HttpContext).Returns(httpContext.Object);
        var service = CreateEmailConfirmationService(dbContext, emailSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, httpAccesor.Object);

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
        var emailSenderMock = new Mock<IEmailSender>();
        var clock = new TestClock();
        var currentClientProviderMock = new Mock<ICurrentClientProvider>();
        var rateLimiter = new Mock<IRateLimitStore>();
        var httpAccesor = new Mock<IHttpContextAccessor>();
        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.Connection.RemoteIpAddress).Returns(new System.Net.IPAddress(123456));
        httpAccesor.Setup(x => x.HttpContext).Returns(httpContext.Object);
        var service = CreateEmailConfirmationService(dbContext, emailSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, httpAccesor.Object);

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
        var emailSenderMock = new Mock<IEmailSender>();
        var clock = new TestClock();
        var currentClientProviderMock = new Mock<ICurrentClientProvider>();
        var rateLimiter = new Mock<IRateLimitStore>();
        var httpAccesor = new Mock<IHttpContextAccessor>();
        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.Connection.RemoteIpAddress).Returns(new System.Net.IPAddress(123456));
        httpAccesor.Setup(x => x.HttpContext).Returns(httpContext.Object);
        var service = CreateEmailConfirmationService(dbContext, emailSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, httpAccesor.Object);

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
        var emailSenderMock = new Mock<IEmailSender>();
        var clock = new TestClock();
        var currentClientProviderMock = new Mock<ICurrentClientProvider>();
        var rateLimiter = new Mock<IRateLimitStore>();
        var httpAccesor = new Mock<IHttpContextAccessor>();
        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.Connection.RemoteIpAddress).Returns(new System.Net.IPAddress(123456));
        httpAccesor.Setup(x => x.HttpContext).Returns(httpContext.Object);
        var rateLimiterOptions = Options.Create(new RateLimitStoreOptions() { MaxFailures = 5, FailureTimeoutSeconds = 120 });
        var service = CreateEmailConfirmationService(dbContext, emailSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, httpAccesor.Object);

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
        IClock clock,
        ICurrentClientProvider currentClientProvider,
        IRateLimitStore rateLimitStore,
        IHttpContextAccessor httpAccessor)
    {
        var options = Options.Create(new EmailVerificationOptions()
        {
            PinLifetimeSeconds = (int)_pinLifetime.TotalSeconds
        });

        var logger = NullLogger<EmailVerificationService>.Instance;

        return new EmailVerificationService(dbContext, emailSender, clock, currentClientProvider, options, logger, rateLimitStore, httpAccessor);
    }
}
