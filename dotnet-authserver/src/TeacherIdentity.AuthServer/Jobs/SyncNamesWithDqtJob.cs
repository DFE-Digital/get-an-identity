using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Polly;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Infrastructure.Http;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Jobs;

public class SyncNamesWithDqtJob : IDisposable
{
    private const int DqtApiRetryCount = 3;
    private const string DebugLogsContainerName = "debug-logs";

    private readonly TimeSpan DqtApiDefaultRetryAfter = TimeSpan.FromSeconds(30);
    private readonly TeacherIdentityServerDbContext _readDbContext;
    private readonly TeacherIdentityServerDbContext _writeDbContext;
    private readonly IDqtApiClient _dqtApiClient;
    private readonly IClock _clock;
    private readonly BlobServiceClient _blobServiceClient;

    public SyncNamesWithDqtJob(
        IDbContextFactory<TeacherIdentityServerDbContext> dbContextFactory,
        IDqtApiClient dqtApiClient,
        IClock clock,
        BlobServiceClient blobServiceClient)
    {
        _readDbContext = dbContextFactory.CreateDbContext();
        _writeDbContext = dbContextFactory.CreateDbContext();
        _dqtApiClient = dqtApiClient;
        _clock = clock;
        _blobServiceClient = blobServiceClient;
    }

    public async Task Execute(CancellationToken cancellationToken)
    {
        await foreach (var userWithTrn in _readDbContext.Users.Where(u => u.Trn != null).AsNoTracking().AsAsyncEnumerable())
        {
            var dqtTeacher = await Policy
                    .Handle<TooManyRequestsException>()
                    .WaitAndRetryAsync(
                        DqtApiRetryCount,
                        sleepDurationProvider: (retryCount, exception, context) =>
                        {
                            var rateLimitingException = exception as TooManyRequestsException;
                            return rateLimitingException!.RetryAfter ?? DqtApiDefaultRetryAfter;
                        },
                        onRetryAsync: (exception, delay, retryCount, context) =>
                        {
                            return Task.CompletedTask;
                        })
                    .ExecuteAsync(() => _dqtApiClient.GetTeacherByTrn(userWithTrn.Trn!, cancellationToken));


            if (!await CheckDqtTeacherRecordIsValid(dqtTeacher))
            {
                continue;
            }

            var changes = UserUpdatedEventChanges.None |
                        (userWithTrn.FirstName != dqtTeacher!.FirstName ? UserUpdatedEventChanges.FirstName : UserUpdatedEventChanges.None) |
                        ((userWithTrn.MiddleName ?? string.Empty) != dqtTeacher!.MiddleName ? UserUpdatedEventChanges.MiddleName : UserUpdatedEventChanges.None) |
                        (userWithTrn.LastName != dqtTeacher!.LastName ? UserUpdatedEventChanges.LastName : UserUpdatedEventChanges.None);

            if (changes == UserUpdatedEventChanges.None)
            {
                continue;
            }

            var userToUpdate = await _writeDbContext.Users.Where(u => u.UserId == userWithTrn.UserId).SingleAsync();
            if (changes.HasFlag(UserUpdatedEventChanges.FirstName))
            {
                userToUpdate.FirstName = dqtTeacher.FirstName;
            }

            if (changes.HasFlag(UserUpdatedEventChanges.MiddleName))
            {
                userToUpdate.MiddleName = dqtTeacher.MiddleName;
            }

            if (changes.HasFlag(UserUpdatedEventChanges.LastName))
            {
                userToUpdate.LastName = dqtTeacher.LastName;
            }

            userToUpdate.Updated = _clock.UtcNow;

            _writeDbContext.AddEvent(new UserUpdatedEvent()
            {
                Source = UserUpdatedEventSource.DqtSynchronization,
                UpdatedByClientId = null,
                UpdatedByUserId = null,
                CreatedUtc = userToUpdate.Updated,
                User = Events.User.FromModel(userToUpdate),
                Changes = changes
            });

            await _writeDbContext.SaveChangesAsync();
        }
    }

    public void Dispose()
    {
        ((IDisposable)_readDbContext).Dispose();
        ((IDisposable)_writeDbContext).Dispose();
    }

    private async Task<bool> CheckDqtTeacherRecordIsValid(TeacherInfo? teacher)
    {
        if (teacher is null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(teacher.FirstName) || string.IsNullOrEmpty(teacher.LastName))
        {
            try
            {
                var blobName = $"{nameof(SyncNamesWithDqtJob)}-{_clock.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid()}.json";
                var blobClient = await GetBlobClient(blobName);
                var debugLog = JsonSerializer.Serialize(teacher);
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(debugLog));
                await blobClient.UploadAsync(stream);
            }
            catch (Exception)
            {
                // Don't want logging issues to abort whole process
            }

            return false;
        }

        return true;
    }

    private async Task<BlobClient> GetBlobClient(string blobName)
    {
        var blobContainerClient = await GetBlobContainerClient();
        return blobContainerClient.GetBlobClient(blobName);
    }

    private async Task<BlobContainerClient> GetBlobContainerClient()
    {
        var blobContainerClient = _blobServiceClient.GetBlobContainerClient(DebugLogsContainerName);
        await blobContainerClient.CreateIfNotExistsAsync();
        return blobContainerClient;
    }
}
