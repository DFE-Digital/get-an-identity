using System.Security.Cryptography;
using Npgsql;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Services;

public class EmailConfirmationService : IEmailConfirmationService
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;
    private readonly ILogger<EmailConfirmationService> _logger;
    private readonly TimeSpan _pinLifetime;

    public EmailConfirmationService(
        TeacherIdentityServerDbContext dbContext,
        IClock clock,
        IConfiguration configuration,
        ILogger<EmailConfirmationService> logger)
    {
        _dbContext = dbContext;
        _clock = clock;
        _logger = logger;
        _pinLifetime = TimeSpan.FromSeconds(configuration.GetValue<int>("EmailConfirmationPinLifetimeSeconds"));
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

        _logger.LogInformation("Generated email confirmation PIN {Pin} for {Email}", pin, email);

        return pin;

        static string GeneratePin() => RandomNumberGenerator.GetInt32(fromInclusive: 100_000, toExclusive: 999_999 + 1).ToString();
    }
}
