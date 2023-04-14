using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services.Notification;
using TeacherIdentity.AuthServer.Services.UserVerification;
using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests.Services;

public class UserVerificationServiceTests : IClassFixture<DbFixture>
{
    private static readonly TimeSpan _pinLifetime = TimeSpan.FromSeconds(120);

    private readonly DbFixture _dbFixture;

    public UserVerificationServiceTests(DbFixture dbFixture)
    {
        _dbFixture = dbFixture;
    }

    [Fact]
    public async Task GenerateEmailPin_AddsPinToDbDeactivatesOlderPinsForEmailAndEmailsPin()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var notificationSenderMock = new Mock<INotificationSender>();
        var clock = new TestClock();
        var currentClientProviderMock = new Mock<ICurrentClientProvider>();
        var rateLimiter = new Mock<IRateLimitStore>();
        var requestClientIpProvider = new TestRequestClientIpProvider();
        var service = CreateUserVerificationService(dbContext, notificationSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

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
        var pinResult = await service.GenerateEmailPin(email);

        // Assert
        Assert.True(pinResult.Succeeded);

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

        notificationSenderMock.Verify(
            mock => mock.SendEmail(
                /* templateId: */ It.IsAny<string>(),
                email,
                It.Is<IReadOnlyDictionary<string, string>>(d =>
                    d["code"] == pinResult.Pin!.ToString() &&
                    d["expiry_minutes"] == "2" &&
                    d["client_name"] == currentClientDisplayName)),
            Times.Once());
    }

    [Fact]
    public async Task VerifyEmailPin_UnknownPin_ReturnsFalse()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var notificationSenderMock = new Mock<INotificationSender>();
        var clock = new TestClock();
        var currentClientProviderMock = new Mock<ICurrentClientProvider>();
        var rateLimiter = new Mock<IRateLimitStore>();
        var requestClientIpProvider = new TestRequestClientIpProvider();
        var service = CreateUserVerificationService(dbContext, notificationSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

        var email = Faker.Internet.Email();

        // The real PIN generation method never generates pins that start with a '0'
        var pin = "012345";

        // Act
        var result = await service.VerifyEmailPin(email, pin);

        // Assert
        Assert.Equal(PinVerificationFailedReasons.Unknown, result);
    }

    [Fact]
    public async Task VerifyEmailPin_ExceededMaxFailureAttempts_ReturnsRateLimitExceeded()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var notificationSenderMock = new Mock<INotificationSender>();
        var clock = new TestClock();
        var currentClientProviderMock = new Mock<ICurrentClientProvider>();
        var rateLimiter = new Mock<IRateLimitStore>();
        rateLimiter.Setup(x => x.IsClientIpBlockedForPinVerification(It.IsAny<string>())).Returns(Task.FromResult(true));
        var requestClientIpProvider = new TestRequestClientIpProvider();
        var service = CreateUserVerificationService(dbContext, notificationSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

        var email = Faker.Internet.Email();

        // The real PIN generation method never generates pins that start with a '0'
        var pin = "012345";

        // Act
        var result = await service.VerifyEmailPin(email, pin);

        // Assert
        Assert.Equal(PinVerificationFailedReasons.RateLimitExceeded, result);
    }

    [Fact]
    public async Task VerifyEmailPin_ExpiredPin_ReturnsFalse()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var notificationSenderMock = new Mock<INotificationSender>();
        var clock = new TestClock();
        var currentClientProviderMock = new Mock<ICurrentClientProvider>();
        var rateLimiter = new Mock<IRateLimitStore>();
        var requestClientIpProvider = new TestRequestClientIpProvider();
        var service = CreateUserVerificationService(dbContext, notificationSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

        var email = Faker.Internet.Email();
        var pinResult = await service.GenerateEmailPin(email);

        clock.AdvanceBy(_pinLifetime);

        // Act
        var result = await service.VerifyEmailPin(email, pinResult.Pin!);

        // Assert
        Assert.True(result.HasFlag(PinVerificationFailedReasons.Expired));
    }

    [Fact]
    public async Task VerifyEmailPin_PinForDifferentEmailAddress_ReturnsFalse()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var notificationSenderMock = new Mock<INotificationSender>();
        var clock = new TestClock();
        var currentClientProviderMock = new Mock<ICurrentClientProvider>();
        var rateLimiter = new Mock<IRateLimitStore>();
        var requestClientIpProvider = new TestRequestClientIpProvider();
        var service = CreateUserVerificationService(dbContext, notificationSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

        var email = Faker.Internet.Email();
        var anotherEmail = Faker.Internet.Email();
        var pinResult = await service.GenerateEmailPin(anotherEmail);

        // Act
        var result = await service.VerifyEmailPin(email, pinResult.Pin!);

        // Assert
        Assert.Equal(PinVerificationFailedReasons.Unknown, result);
    }

    [Fact]
    public async Task VerifyEmailPin_PinIsNotActive_ReturnsFalse()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var notificationSenderMock = new Mock<INotificationSender>();
        var clock = new TestClock();
        var currentClientProviderMock = new Mock<ICurrentClientProvider>();
        var rateLimiter = new Mock<IRateLimitStore>();
        var requestClientIpProvider = new TestRequestClientIpProvider();
        var service = CreateUserVerificationService(dbContext, notificationSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

        var email = Faker.Internet.Email();
        var pinResult = await service.GenerateEmailPin(email);

        var ecp = await dbContext.EmailConfirmationPins.SingleAsync(p => p.Email == email && p.Pin == pinResult.Pin);
        ecp.IsActive = false;
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.VerifyEmailPin(email, pinResult.Pin!);

        // Assert
        Assert.Equal(PinVerificationFailedReasons.NotActive, result);
    }

    [Fact]
    public async Task VerifyEmailPin_ValidPin_DeactivatesPinAndReturnsTrue()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var notificationSenderMock = new Mock<INotificationSender>();
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
        var service = CreateUserVerificationService(dbContext, notificationSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

        var email = Faker.Internet.Email();
        var pinResult = await service.GenerateEmailPin(email);

        // Act
        var result = await service.VerifyEmailPin(email, pinResult.Pin!);

        // Assert
        Assert.Equal(PinVerificationFailedReasons.None, result);

        var emailConfirmationPin = await dbContext.EmailConfirmationPins.Where(p => p.Email == email && p.Pin == pinResult.Pin).SingleOrDefaultAsync();
        Assert.False(emailConfirmationPin?.IsActive);
        Assert.Equal(clock.UtcNow, emailConfirmationPin?.VerifiedOn);
    }

    [Fact]
    public async Task GenerateSmsPin_AddsPinToDbDeactivatesOlderPinsForSmsAndTextsPin()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var notificationSenderMock = new Mock<INotificationSender>();
        var clock = new TestClock();
        var currentClientProviderMock = new Mock<ICurrentClientProvider>();
        var rateLimiter = new Mock<IRateLimitStore>();
        var requestClientIpProvider = new TestRequestClientIpProvider();
        var service = CreateUserVerificationService(dbContext, notificationSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

        var currentClientDisplayName = "Test app";
        currentClientProviderMock.Setup(mock => mock.GetCurrentClient()).ReturnsAsync(new Application() { DisplayName = currentClientDisplayName });

        var mobileNumber = _dbFixture.TestData.GenerateUniqueMobileNumber();
        var parsedMobileNumber = MobileNumber.Parse(mobileNumber);

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            dbContext.SmsConfirmationPins.Add(new SmsConfirmationPin()
            {
                MobileNumber = parsedMobileNumber,
                Expires = clock.UtcNow + _pinLifetime,
                IsActive = true,
                VerifiedOn = null,
                Pin = "01234"
            });

            await dbContext.SaveChangesAsync();
        });

        // Act
        var pinResult = await service.GenerateSmsPin(parsedMobileNumber);

        // Assert
        var smsConfirmationPins = await dbContext.SmsConfirmationPins.Where(p => p.MobileNumber == parsedMobileNumber)
            .OrderBy(p => p.Expires)
            .ToListAsync();

        Assert.Collection(
            smsConfirmationPins,
            oldPin =>
            {
                Assert.False(oldPin.IsActive);
            },
            newPin =>
            {
                Assert.Null(newPin.VerifiedOn);
                Assert.Equal(parsedMobileNumber, newPin.MobileNumber);
                Assert.Equal(pinResult.Pin, newPin.Pin);
                Assert.Equal(clock.UtcNow + _pinLifetime, newPin.Expires);
            });

        notificationSenderMock.Verify(
            mock => mock.SendSms(
                /* templateId: */ It.IsAny<string>(),
                parsedMobileNumber.ToString(),
                It.Is<IReadOnlyDictionary<string, string>>(d => d["code"] == pinResult.Pin!.ToString())),
            Times.Once());
    }

    [Fact]
    public async Task VerifySmsPin_UnknownPin_ReturnsFalse()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var notificationSenderMock = new Mock<INotificationSender>();
        var clock = new TestClock();
        var currentClientProviderMock = new Mock<ICurrentClientProvider>();
        var rateLimiter = new Mock<IRateLimitStore>();
        var requestClientIpProvider = new TestRequestClientIpProvider();
        var service = CreateUserVerificationService(dbContext, notificationSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

        var mobileNumber = _dbFixture.TestData.GenerateUniqueMobileNumber();

        // The real PIN generation method never generates pins that start with a '0'
        var pin = "012345";

        // Act
        var result = await service.VerifySmsPin(MobileNumber.Parse(mobileNumber), pin);

        // Assert
        Assert.Equal(PinVerificationFailedReasons.Unknown, result);
    }

    [Fact]
    public async Task VerifySmsPin_ExceededMaxFailureAttempts_ReturnsRateLimitExceeded()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var notificationSenderMock = new Mock<INotificationSender>();
        var clock = new TestClock();
        var currentClientProviderMock = new Mock<ICurrentClientProvider>();
        var rateLimiter = new Mock<IRateLimitStore>();
        rateLimiter.Setup(x => x.IsClientIpBlockedForPinVerification(It.IsAny<string>())).Returns(Task.FromResult(true));
        var requestClientIpProvider = new TestRequestClientIpProvider();
        var service = CreateUserVerificationService(dbContext, notificationSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

        var mobileNumber = _dbFixture.TestData.GenerateUniqueMobileNumber();

        // The real PIN generation method never generates pins that start with a '0'
        var pin = "012345";

        // Act
        var result = await service.VerifySmsPin(MobileNumber.Parse(mobileNumber), pin);

        // Assert
        Assert.Equal(PinVerificationFailedReasons.RateLimitExceeded, result);
    }

    [Fact]
    public async Task VerifySmsPin_ExpiredPin_ReturnsFalse()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var notificationSenderMock = new Mock<INotificationSender>();
        var clock = new TestClock();
        var currentClientProviderMock = new Mock<ICurrentClientProvider>();
        var rateLimiter = new Mock<IRateLimitStore>();
        var requestClientIpProvider = new TestRequestClientIpProvider();
        var service = CreateUserVerificationService(dbContext, notificationSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

        var mobileNumber = _dbFixture.TestData.GenerateUniqueMobileNumber();
        var pinResult = await service.GenerateSmsPin(MobileNumber.Parse(mobileNumber));

        clock.AdvanceBy(_pinLifetime);

        // Act
        var result = await service.VerifySmsPin(MobileNumber.Parse(mobileNumber), pinResult.Pin!);

        // Assert
        Assert.True(result.HasFlag(PinVerificationFailedReasons.Expired));
    }

    [Fact]
    public async Task VerifySmsPin_PinForDifferentMobileNumber_ReturnsFalse()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var notificationSenderMock = new Mock<INotificationSender>();
        var clock = new TestClock();
        var currentClientProviderMock = new Mock<ICurrentClientProvider>();
        var rateLimiter = new Mock<IRateLimitStore>();
        var requestClientIpProvider = new TestRequestClientIpProvider();
        var service = CreateUserVerificationService(dbContext, notificationSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

        var mobileNumber = _dbFixture.TestData.GenerateUniqueMobileNumber();
        var anotherMobileNumber = _dbFixture.TestData.GenerateUniqueMobileNumber();
        var pinResult = await service.GenerateSmsPin(MobileNumber.Parse(anotherMobileNumber));

        // Act
        var result = await service.VerifySmsPin(MobileNumber.Parse(mobileNumber), pinResult.Pin!);

        // Assert
        Assert.Equal(PinVerificationFailedReasons.Unknown, result);
    }

    [Fact]
    public async Task VerifySmsPin_PinIsNotActive_ReturnsFalse()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var notificationSenderMock = new Mock<INotificationSender>();
        var clock = new TestClock();
        var currentClientProviderMock = new Mock<ICurrentClientProvider>();
        var rateLimiter = new Mock<IRateLimitStore>();
        var requestClientIpProvider = new TestRequestClientIpProvider();
        var service = CreateUserVerificationService(dbContext, notificationSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

        var mobileNumber = _dbFixture.TestData.GenerateUniqueMobileNumber();
        var parsedMobileNumber = MobileNumber.Parse(mobileNumber);
        var pinResult = await service.GenerateSmsPin(parsedMobileNumber);

        var smsConfirmationPin = await dbContext.SmsConfirmationPins.SingleAsync(p => p.MobileNumber == parsedMobileNumber && p.Pin == pinResult.Pin);
        smsConfirmationPin.IsActive = false;
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.VerifySmsPin(MobileNumber.Parse(mobileNumber), pinResult.Pin!);

        // Assert
        Assert.Equal(PinVerificationFailedReasons.NotActive, result);
    }

    [Fact]
    public async Task VerifySmsPin_ValidPin_DeactivatesPinAndReturnsTrue()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var notificationSenderMock = new Mock<INotificationSender>();
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
        var service = CreateUserVerificationService(dbContext, notificationSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

        var mobileNumber = _dbFixture.TestData.GenerateUniqueMobileNumber();
        var parsedMobileNumber = MobileNumber.Parse(mobileNumber);
        var pinResult = await service.GenerateSmsPin(parsedMobileNumber);

        // Act
        var result = await service.VerifySmsPin(MobileNumber.Parse(mobileNumber), pinResult.Pin!);

        // Assert
        Assert.Equal(PinVerificationFailedReasons.None, result);

        var smsConfirmationPin = await dbContext.SmsConfirmationPins.Where(p => p.MobileNumber == parsedMobileNumber && p.Pin == pinResult.Pin).SingleOrDefaultAsync();
        Assert.False(smsConfirmationPin?.IsActive);
        Assert.Equal(clock.UtcNow, smsConfirmationPin?.VerifiedOn);
    }

    private UserVerificationService CreateUserVerificationService(
        TeacherIdentityServerDbContext dbContext,
        INotificationSender notificationSender,
        IClock clock,
        ICurrentClientProvider currentClientProvider,
        IRateLimitStore rateLimitStore,
        IRequestClientIpProvider clientIpProvider)
    {
        var options = Options.Create(new UserVerificationOptions()
        {
            PinLifetimeSeconds = (int)_pinLifetime.TotalSeconds
        });

        var logger = NullLogger<UserVerificationService>.Instance;

        return new UserVerificationService(dbContext, notificationSender, clock, currentClientProvider, options, logger, rateLimitStore, clientIpProvider);
    }
}
