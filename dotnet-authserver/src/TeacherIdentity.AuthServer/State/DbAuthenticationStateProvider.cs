using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Polly;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.State;

public sealed class DbAuthenticationStateProvider : IAuthenticationStateProvider, IAsyncDisposable
{
    private const string JourneyIdsCookieName = "tis-session2";

    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;
    private readonly ILogger<DbAuthenticationStateProvider> _logger;
    private readonly IDataProtector _dataProtector;
    private IDbContextTransaction? _transaction;

    public DbAuthenticationStateProvider(
        TeacherIdentityServerDbContext dbContext,
        IClock clock,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<DbAuthenticationStateProvider> logger)
    {
        _dbContext = dbContext;
        _clock = clock;
        _logger = logger;
        _dataProtector = dataProtectionProvider.CreateProtector(nameof(DbAuthenticationStateProvider));
    }

    public async Task<AuthenticationState?> GetAuthenticationState(HttpContext httpContext)
    {
        var userJourneyIds = GetUserJourneyIdsFromCookie(httpContext);

        _transaction ??= await CreateTransaction();

        var retryPolicy = Policy
            .Handle<InvalidOperationException>(IsPostgresSerializationError)
            .RetryAsync(
                3,
                onRetryAsync: async (ex, retryCount) =>
                {
                    // Transaction will be aborted at this point - create a new one
                    await _transaction.DisposeAsync();
                    _transaction = await CreateTransaction();
                });

        if (httpContext.Request.Query.TryGetValue(AuthenticationStateMiddleware.IdQueryParameterName, out var asidStr) &&
            Guid.TryParse(asidStr, out var journeyId) &&
            userJourneyIds.Contains(journeyId))
        {
            return await retryPolicy.ExecuteAsync(async () =>
            {
                using var suppressScope = SentryErrors.Suppress(IsPostgresSerializationError);

                var dbAuthState = await _dbContext.AuthenticationStates.FromSqlInterpolated(
                        $"select * from authentication_states where journey_id = {journeyId} for update")
                    .SingleOrDefaultAsync();

                if (dbAuthState is not null)
                {
                    try
                    {
                        return AuthenticationState.Deserialize(dbAuthState.Payload);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed deserializing {nameof(AuthenticationState)}.");
                    }
                }

                return null;
            });
        }

        return null;

        static bool IsPostgresSerializationError(Exception ex) =>
            ex.InnerException is PostgresException pex && pex.SqlState == PostgresErrorCodes.SerializationFailure;

        Task<IDbContextTransaction> CreateTransaction() =>
            _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
    }

    public async Task SetAuthenticationState(HttpContext httpContext, AuthenticationState authenticationState)
    {
        Debug.Assert(_transaction is not null);

        var journeyId = authenticationState.JourneyId;
        var serializedState = authenticationState.Serialize();

        var journeyIdParameter = new NpgsqlParameter("p1", NpgsqlTypes.NpgsqlDbType.Uuid) { Value = journeyId };
        var payloadParameter = new NpgsqlParameter("p3", NpgsqlTypes.NpgsqlDbType.Jsonb) { Value = serializedState };
        var nowParameter = new NpgsqlParameter("p4", NpgsqlTypes.NpgsqlDbType.TimestampTz) { Value = _clock.UtcNow };

        await _dbContext.Database.ExecuteSqlRawAsync(
            """
            insert into authentication_states (journey_id, payload, created, last_accessed)
            values (@p1, @p3, @p4, @p4)
            on conflict (journey_id) do update set payload = excluded.payload, last_accessed = excluded.last_accessed;
            """,
            journeyIdParameter,
            payloadParameter,
            nowParameter);

        await _transaction.CommitAsync();

        EnsureUserJourneyIdInCookie(httpContext, journeyId);
    }

    private IEnumerable<Guid> GetUserJourneyIdsFromCookie(HttpContext httpContext)
    {
        if (httpContext.Request.Cookies.TryGetValue(JourneyIdsCookieName, out var journeyIdsCookieValue))
        {
            try
            {
                var serialized = _dataProtector.Unprotect(journeyIdsCookieValue);
                return JsonSerializer.Deserialize<JourneyIdsCookie>(serialized)?.JourneyIds ?? Enumerable.Empty<Guid>();
            }
            catch (CryptographicException)
            {
            }
        }

        return Enumerable.Empty<Guid>();
    }

    private void EnsureUserJourneyIdInCookie(HttpContext httpContext, Guid journeyId)
    {
        var userJourneyIds = GetUserJourneyIdsFromCookie(httpContext);

        if (!userJourneyIds.Contains(journeyId))
        {
            var serialized = JsonSerializer.Serialize(new JourneyIdsCookie { JourneyIds = userJourneyIds.Append(journeyId) });
            var protcted = _dataProtector.Protect(serialized);

            httpContext.Response.OnStarting(() =>
            {
                httpContext.Response.Cookies.Append(JourneyIdsCookieName, protcted);
                return Task.CompletedTask;
            });
        }
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
        }
    }

    private class JourneyIdsCookie
    {
        public required IEnumerable<Guid> JourneyIds { get; set; }
    }
}
