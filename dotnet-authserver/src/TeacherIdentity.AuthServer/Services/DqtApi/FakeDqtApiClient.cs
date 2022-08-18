using System.Text.Json;

namespace TeacherIdentity.AuthServer.Services.DqtApi;

/// <summary>
/// A fake implementation of <see cref="IDqtApiClient"/> that writes UserId/TRN associations to a JSON file.
/// </summary>
public class FakeDqtApiClient : IDqtApiClient
{
    private readonly object _gate = new object();

    public Task<DqtTeacherIdentityInfo?> GetTeacherIdentityInfo(Guid userId)
    {
        DqtTeacherIdentityInfo? result = null;

        WithDatabaseFile(db =>
        {
            if (db.UserIdTrnAssociations.TryGetValue(userId, out var trn))
            {
                result = new DqtTeacherIdentityInfo()
                {
                    Trn = trn,
                    UserId = userId
                };
            }
        });

        return Task.FromResult(result);
    }

    public Task SetTeacherIdentityInfo(DqtTeacherIdentityInfo info)
    {
        WithDatabaseFile(db =>
        {
            db.UserIdTrnAssociations[info.UserId] = info.Trn;
        });

        return Task.CompletedTask;
    }

    private void WithDatabaseFile(Action<FakeDatabaseFile> action)
    {
        lock (_gate)
        {
            var dbFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TeacherIdentity.AuthServer", "dqt-api.json");
            Directory.CreateDirectory(Path.GetDirectoryName(dbFilePath)!);

            FakeDatabaseFile dbFile;

            if (File.Exists(dbFilePath))
            {
                dbFile = JsonSerializer.Deserialize<FakeDatabaseFile>(File.ReadAllText(dbFilePath))!;
            }
            else
            {
                dbFile = new FakeDatabaseFile();
            }

            action(dbFile);

            File.WriteAllText(dbFilePath, JsonSerializer.Serialize(dbFile));
        }
    }

    private class FakeDatabaseFile
    {
        /// <summary>
        /// A mapping from user IDs to TRNs.
        /// </summary>
        public Dictionary<Guid, string> UserIdTrnAssociations { get; set; } = new();
    }
}
