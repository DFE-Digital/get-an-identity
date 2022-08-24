using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services.Email;

namespace TeacherIdentity.AuthServer.Services.EmailVerification;

public class EmailVerificationService : IEmailVerificationService
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IEmailSender _emailSender;
    private readonly IClock _clock;
    private readonly ICurrentClientProvider _currentClientProvider;
    private readonly ILogger<EmailVerificationService> _logger;
    private readonly TimeSpan _pinLifetime;

    public EmailVerificationService(
        TeacherIdentityServerDbContext dbContext,
        IEmailSender emailSender,
        IClock clock,
        ICurrentClientProvider currentClientProvider,
        IOptions<EmailVerificationOptions> optionsAccessor,
        ILogger<EmailVerificationService> logger)
    {
        _dbContext = dbContext;
        _emailSender = emailSender;
        _clock = clock;
        _currentClientProvider = currentClientProvider;
        _logger = logger;
        _pinLifetime = TimeSpan.FromSeconds(optionsAccessor.Value.PinLifetimeSeconds);
    }

    public async Task<string> GeneratePin(string email)
    {
        // Generate a random PIN then try to insert it into the DB for the specified email address.
        // If it's a duplicate, repeat...

        var expires = _clock.UtcNow.Add(_pinLifetime);

        string pin;

        while (true)
        {
            pin = GeneratePin();

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

        var emailSubject = "Confirm your email address";
        var emailBody = $"{pin} is your {clientDisplayName} security code";
        await _emailSender.SendEmail(email, emailSubject, emailBody);

        _logger.LogInformation("Generated email confirmation PIN {Pin} for {Email}", pin, email);

        return pin;

        static string GeneratePin() => RandomNumberGenerator.GetInt32(fromInclusive: 100_00, toExclusive: 99_999 + 1).ToString();
    }

    public async Task<PinVerificationFailedReasons> VerifyPin(string email, string pin)
    {
        var emailConfirmationPin = await _dbContext.EmailConfirmationPins
            .Where(e => e.Email == email && e.Pin == pin)
            .SingleOrDefaultAsync();

        if (emailConfirmationPin is null)
        {
            return PinVerificationFailedReasons.Unknown;
        }

        if (!emailConfirmationPin.IsActive)
        {
            return PinVerificationFailedReasons.NotActive;
        }

        if (emailConfirmationPin.Expires <= _clock.UtcNow)
        {
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
}
