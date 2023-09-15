using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services.Notification;

namespace TeacherIdentity.AuthServer.Services.UserVerification;

public class UserVerificationService : IUserVerificationService
{
    private const string EmailConfirmationTemplateId = "de2ead50-3213-4cd4-9c79-218e534c98ad";
    private const string MobilePhoneConfirmationTemplateId = "2a09c36e-5670-4f69-a315-21976ee93d46";

    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly INotificationSender _notificationSender;
    private readonly IClock _clock;
    private readonly ICurrentClientProvider _currentClientProvider;
    private readonly ILogger<UserVerificationService> _logger;
    private readonly TimeSpan _pinLifetime;
    private readonly IRateLimitStore _rateLimiter;
    private readonly IRequestClientIpProvider _clientIpProvider;

    private static string GeneratePin() => RandomNumberGenerator.GetInt32(fromInclusive: 10_000, toExclusive: 99_999 + 1).ToString();

    public UserVerificationService(
        TeacherIdentityServerDbContext dbContext,
        INotificationSender notificationSender,
        IClock clock,
        ICurrentClientProvider currentClientProvider,
        IOptions<UserVerificationOptions> optionsAccessor,
        ILogger<UserVerificationService> logger,
        IRateLimitStore rateLimiter,
        IRequestClientIpProvider clientIpProvider)
    {
        _dbContext = dbContext;
        _notificationSender = notificationSender;
        _clock = clock;
        _currentClientProvider = currentClientProvider;
        _logger = logger;
        _pinLifetime = TimeSpan.FromSeconds(optionsAccessor.Value.PinLifetimeSeconds);
        _rateLimiter = rateLimiter;
        _clientIpProvider = clientIpProvider;
    }

    public async Task<PinGenerationResult> GenerateEmailPin(string email)
    {
        var ip = _clientIpProvider.GetClientIpAddress();
        if (await _rateLimiter.IsClientIpBlockedForPinGeneration(ip))
        {
            return PinGenerationResult.Failed(PinGenerationFailedReason.RateLimitExceeded);
        }

        // Generate a random PIN then try to insert it into the DB for the specified email address.
        // If it's a duplicate, repeat...

        var expires = _clock.UtcNow.Add(_pinLifetime);

        string pin;

        while (true)
        {
            pin = GeneratePin();

            //always track pin generation counts for ip address
            await _rateLimiter.AddPinGeneration(ip);

            try
            {
                _dbContext.EmailConfirmationPins.Add(new EmailConfirmationPin()
                {
                    Email = email,
                    Expires = expires,
                    IsActive = true,
                    Pin = pin
                });

                // Remove any other active PINs for this email
                await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"update email_confirmation_pins set is_active = false where email = {email} and expires > {_clock.UtcNow} and is_active = true;");

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex) when (
                ex.InnerException is PostgresException postgresException &&
                postgresException.SqlState == PostgresErrorCodes.UniqueViolation &&
                postgresException.ConstraintName == "ix_email_confirmation_pins_email_pin")
            {
                // Duplicate PIN
                _dbContext.ChangeTracker.Clear();
                continue;
            }

            break;
        }

        var client = await _currentClientProvider.GetCurrentClient();
        var clientDisplayName = client?.DisplayName ?? "Get an identity to access Teacher Services";

        var emailPersonalization = new Dictionary<string, string>()
        {
            { "code", pin },
            { "expiry_minutes", ((int)_pinLifetime.TotalMinutes).ToString() },
            { "client_name", clientDisplayName },
        };

        try
        {
            await _notificationSender.SendEmail(EmailConfirmationTemplateId, email, emailPersonalization);
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("ValidationError"))
            {
                return PinGenerationResult.Failed(PinGenerationFailedReason.InvalidAddress);
            }

            throw;
        }

        _logger.LogInformation("Generated email confirmation PIN {Pin} for {Email}", pin, email);

        return PinGenerationResult.Success(pin);
    }

    public async Task<PinVerificationFailedReasons> VerifyEmailPin(string email, string pin)
    {
        var ip = _clientIpProvider.GetClientIpAddress();
        if (await _rateLimiter.IsClientIpBlockedForPinVerification(ip!))
        {
            return PinVerificationFailedReasons.RateLimitExceeded;
        }

        var emailConfirmationPin = await _dbContext.EmailConfirmationPins
            .Where(e => e.Email == email && e.Pin == pin)
            .SingleOrDefaultAsync();

        return await VerifyPin(emailConfirmationPin, ip);
    }

    public async Task<PinGenerationResult> GenerateSmsPin(MobileNumber mobileNumber)
    {
        var ip = _clientIpProvider.GetClientIpAddress();
        if (await _rateLimiter.IsClientIpBlockedForPinGeneration(ip))
        {
            return PinGenerationResult.Failed(PinGenerationFailedReason.RateLimitExceeded);
        }

        // Generate a random PIN then try to insert it into the DB for the specified mobile number.
        // If it's a duplicate, repeat...

        var expires = _clock.UtcNow.Add(_pinLifetime);

        string pin;

        while (true)
        {
            pin = GeneratePin();

            //always track pin generation counts for ip address
            await _rateLimiter.AddPinGeneration(ip);

            try
            {
                _dbContext.SmsConfirmationPins.Add(new SmsConfirmationPin()
                {
                    MobileNumber = mobileNumber,
                    Expires = expires,
                    IsActive = true,
                    Pin = pin
                });

                // Remove any other active PINs for this email
#pragma warning disable IDE0071 // Simplify interpolation
                await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"update sms_confirmation_pins set is_active = false where mobile_number = {mobileNumber.ToString()} and expires > {_clock.UtcNow} and is_active = true;");
#pragma warning restore IDE0071 // Simplify interpolation

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex) when (
                ex.InnerException is PostgresException postgresException &&
                postgresException.SqlState == PostgresErrorCodes.UniqueViolation &&
                postgresException.ConstraintName == "ix_sms_confirmation_pins_mobile_number_pin")
            {
                // Duplicate PIN
                _dbContext.ChangeTracker.Clear();
                continue;
            }

            break;
        }

        var smsPersonalization = new Dictionary<string, string>()
        {
            { "code", pin }
        };

        try
        {
            await _notificationSender.SendSms(MobilePhoneConfirmationTemplateId, mobileNumber.ToString(), smsPersonalization);
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("ValidationError"))
            {
                return PinGenerationResult.Failed(PinGenerationFailedReason.InvalidAddress);
            }

            throw;
        }

        _logger.LogInformation("Generated SMS confirmation PIN {Pin} for {Number}", pin, mobileNumber);

        return PinGenerationResult.Success(pin);
    }

    public async Task<PinVerificationFailedReasons> VerifySmsPin(MobileNumber mobileNumber, string pin)
    {
        var ip = _clientIpProvider.GetClientIpAddress();
        if (await _rateLimiter.IsClientIpBlockedForPinVerification(ip!))
        {
            return PinVerificationFailedReasons.RateLimitExceeded;
        }

        var smsConfirmationPin = await _dbContext.SmsConfirmationPins
            .Where(e => e.MobileNumber == mobileNumber && e.Pin == pin)
            .SingleOrDefaultAsync();

        return await VerifyPin(smsConfirmationPin, ip);
    }

    private async Task<PinVerificationFailedReasons> VerifyPin(IConfirmationPin? pin, string ip)
    {
        if (pin is null)
        {
            await _rateLimiter.AddFailedPinVerification(ip!);
            return PinVerificationFailedReasons.Unknown;
        }

        if (!pin.IsActive)
        {
            await _rateLimiter.AddFailedPinVerification(ip!);
            return PinVerificationFailedReasons.NotActive;
        }

        if (pin.Expires <= _clock.UtcNow)
        {
            await _rateLimiter.AddFailedPinVerification(ip!);
            var reasons = PinVerificationFailedReasons.Expired;

            var expiredLessThanTwoHoursAgo = (_clock.UtcNow - pin.Expires) < TimeSpan.FromHours(2);
            if (expiredLessThanTwoHoursAgo)
            {
                reasons |= PinVerificationFailedReasons.ExpiredLessThanTwoHoursAgo;
            }

            return reasons;
        }

        // PIN is good
        // Deactivate the PIN so it cannot be used again
        pin.VerifiedOn = _clock.UtcNow;
        pin.IsActive = false;
        await _dbContext.SaveChangesAsync();

        return PinVerificationFailedReasons.None;
    }
}
