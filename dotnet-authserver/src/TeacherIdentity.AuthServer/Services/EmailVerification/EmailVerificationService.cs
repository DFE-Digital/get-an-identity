using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services.Email;
using Exception = System.Exception;

namespace TeacherIdentity.AuthServer.Services.EmailVerification;

public class EmailVerificationService : IEmailVerificationService
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IEmailSender _emailSender;
    private readonly IClock _clock;
    private readonly ICurrentClientProvider _currentClientProvider;
    private readonly ILogger<EmailVerificationService> _logger;
    private readonly TimeSpan _pinLifetime;
    private readonly IRateLimitStore _rateLimiter;
    private readonly IRequestClientIpProvider _clientIpProvider;

    public EmailVerificationService(
        TeacherIdentityServerDbContext dbContext,
        IEmailSender emailSender,
        IClock clock,
        ICurrentClientProvider currentClientProvider,
        IOptions<EmailVerificationOptions> optionsAccessor,
        ILogger<EmailVerificationService> logger,
        IRateLimitStore rateLimiter,
        IRequestClientIpProvider clientIpProvider)
    {
        _dbContext = dbContext;
        _emailSender = emailSender;
        _clock = clock;
        _currentClientProvider = currentClientProvider;
        _logger = logger;
        _pinLifetime = TimeSpan.FromSeconds(optionsAccessor.Value.PinLifetimeSeconds);
        _rateLimiter = rateLimiter;
        _clientIpProvider = clientIpProvider;
    }

    public async Task<PinGenerationResult> GeneratePin(string email)
    {
        var ip = _clientIpProvider.GetClientIpAddress();
        if (await _rateLimiter.IsClientIpBlockedForPinGeneration(ip))
        {
            return PinGenerationResult.Failed(PinGenerationFailedReasons.RateLimitExceeded);
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
                continue;
            }

            break;
        }

        var client = await _currentClientProvider.GetCurrentClient();
        var clientDisplayName = client?.DisplayName ?? "Get an identity to access Teacher Services";

        var emailBody = GetEmailBody(pin, (int)_pinLifetime.TotalMinutes, clientDisplayName);

        try
        {
            await _emailSender.SendEmail(email, EmailSubject, emailBody);
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("ValidationError"))
            {
                return PinGenerationResult.Failed(PinGenerationFailedReasons.InvalidEmail);
            }

            throw;
        }

        _logger.LogInformation("Generated email confirmation PIN {Pin} for {Email}", pin, email);

        return PinGenerationResult.Success(pin);

        static string GeneratePin() => RandomNumberGenerator.GetInt32(fromInclusive: 10_000, toExclusive: 99_999 + 1).ToString();
    }

    public async Task<PinVerificationFailedReasons> VerifyPin(string email, string pin)
    {
        var ip = _clientIpProvider.GetClientIpAddress();
        if (await _rateLimiter.IsClientIpBlockedForPinVerification(ip!))
        {
            return PinVerificationFailedReasons.RateLimitExceeded;
        }

        var emailConfirmationPin = await _dbContext.EmailConfirmationPins
            .Where(e => e.Email == email && e.Pin == pin)
            .SingleOrDefaultAsync();

        if (emailConfirmationPin is null)
        {
            await _rateLimiter.AddFailedPinVerification(ip!);
            return PinVerificationFailedReasons.Unknown;
        }

        if (!emailConfirmationPin.IsActive)
        {
            await _rateLimiter.AddFailedPinVerification(ip!);
            return PinVerificationFailedReasons.NotActive;
        }

        if (emailConfirmationPin.Expires <= _clock.UtcNow)
        {
            await _rateLimiter.AddFailedPinVerification(ip!);
            var reasons = PinVerificationFailedReasons.Expired;

            var expiredLessThanTwoHoursAgo = (_clock.UtcNow - emailConfirmationPin.Expires) < TimeSpan.FromHours(2);
            if (expiredLessThanTwoHoursAgo)
            {
                reasons |= PinVerificationFailedReasons.ExpiredLessThanTwoHoursAgo;
            }

            return reasons;
        }

        // PIN is good
        // Deactivate the PIN so it cannot be used again
        emailConfirmationPin!.VerifiedOn = _clock.UtcNow;
        emailConfirmationPin!.IsActive = false;
        await _dbContext.SaveChangesAsync();

        return PinVerificationFailedReasons.None;
    }

    private const string EmailSubject = "Confirm your email address";
    private string GetEmailBody(string pin, int minutes, string clientName) => $"Use this code to confirm your email address:\n\n" +
                    $"{pin}\n\n" +
                    $"The code will expire after {minutes} minutes.\n\n" +
                    $"This email address has been used for {clientName}.\n\n" +
                    $"If this was not you, you can ignore this email.\n\n" +
                    $"Department for Education";
}
