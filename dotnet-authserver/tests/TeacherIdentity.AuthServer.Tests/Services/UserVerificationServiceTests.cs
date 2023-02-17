using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services.Notification;
using TeacherIdentity.AuthServer.Services.UserVerification;
using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests.Services;

[Collection(nameof(DisableParallelization))]  // Changes the clock
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
        var service = CreateUserConfirmationService(dbContext, notificationSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

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

        notificationSenderMock.Verify(mock => mock.SendEmail(email, expectedEmailSubject, expectedEmailBody), Times.Once());
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
        var service = CreateUserConfirmationService(dbContext, notificationSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

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
        var service = CreateUserConfirmationService(dbContext, notificationSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

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
        var service = CreateUserConfirmationService(dbContext, notificationSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

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
        var service = CreateUserConfirmationService(dbContext, notificationSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

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
        var service = CreateUserConfirmationService(dbContext, notificationSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

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
        var service = CreateUserConfirmationService(dbContext, notificationSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

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
        var service = CreateUserConfirmationService(dbContext, notificationSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

        var currentClientDisplayName = "Test app";
        currentClientProviderMock.Setup(mock => mock.GetCurrentClient()).ReturnsAsync(new Application() { DisplayName = currentClientDisplayName });

        var mobileNumber = Faker.Phone.Number();

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            dbContext.SmsConfirmationPins.Add(new SmsConfirmationPin()
            {
                MobileNumber = mobileNumber,
                Expires = clock.UtcNow + _pinLifetime,
                IsActive = true,
                VerifiedOn = null,
                Pin = "01234"
            });

            await dbContext.SaveChangesAsync();
        });

        // Act
        var pinResult = await service.GenerateSmsPin(mobileNumber);

        // Assert
        var smsConfirmationPins = await dbContext.SmsConfirmationPins.Where(p => p.MobileNumber == mobileNumber)
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
                Assert.Equal(mobileNumber, newPin.MobileNumber);
                Assert.Equal(pinResult.Pin, newPin.Pin);
                Assert.Equal(clock.UtcNow + _pinLifetime, newPin.Expires);
            });

        var expectedSmsMessage = $"{pinResult.Pin} is your Teaching Services Account authentication code";

        notificationSenderMock.Verify(mock => mock.SendSms(mobileNumber, expectedSmsMessage), Times.Once());
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
        var service = CreateUserConfirmationService(dbContext, notificationSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

        var mobileNumber = Faker.Phone.Number();

        // The real PIN generation method never generates pins that start with a '0'
        var pin = "012345";

        // Act
        var result = await service.VerifySmsPin(mobileNumber, pin);

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
        var service = CreateUserConfirmationService(dbContext, notificationSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

        var mobileNumber = Faker.Phone.Number();

        // The real PIN generation method never generates pins that start with a '0'
        var pin = "012345";

        // Act
        var result = await service.VerifySmsPin(mobileNumber, pin);

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
        var service = CreateUserConfirmationService(dbContext, notificationSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

        var mobileNumber = Faker.Phone.Number();
        var pinResult = await service.GenerateSmsPin(mobileNumber);

        clock.AdvanceBy(_pinLifetime);

        // Act
        var result = await service.VerifySmsPin(mobileNumber, pinResult.Pin!);

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
        var service = CreateUserConfirmationService(dbContext, notificationSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

        var mobileNumber = Faker.Phone.Number();
        var anotherMobileNumber = Faker.Phone.Number();
        var pinResult = await service.GenerateSmsPin(anotherMobileNumber);

        // Act
        var result = await service.VerifySmsPin(mobileNumber, pinResult.Pin!);

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
        var service = CreateUserConfirmationService(dbContext, notificationSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

        var mobileNumber = Faker.Phone.Number();
        var pinResult = await service.GenerateSmsPin(mobileNumber);

        var smsConfirmationPin = await dbContext.SmsConfirmationPins.SingleAsync(p => p.MobileNumber == mobileNumber && p.Pin == pinResult.Pin);
        smsConfirmationPin.IsActive = false;
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.VerifySmsPin(mobileNumber, pinResult.Pin!);

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
        var service = CreateUserConfirmationService(dbContext, notificationSenderMock.Object, clock, currentClientProviderMock.Object, rateLimiter.Object, requestClientIpProvider);

        var mobileNumber = Faker.Phone.Number();
        var pinResult = await service.GenerateSmsPin(mobileNumber);

        // Act
        var result = await service.VerifySmsPin(mobileNumber, pinResult.Pin!);

        // Assert
        Assert.Equal(PinVerificationFailedReasons.None, result);

        var smsConfirmationPin = await dbContext.SmsConfirmationPins.Where(p => p.MobileNumber == mobileNumber && p.Pin == pinResult.Pin).SingleOrDefaultAsync();
        Assert.False(smsConfirmationPin?.IsActive);
        Assert.Equal(clock.UtcNow, smsConfirmationPin?.VerifiedOn);
    }

    private UserVerificationService CreateUserConfirmationService(
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