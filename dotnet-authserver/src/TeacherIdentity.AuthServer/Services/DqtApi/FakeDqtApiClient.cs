using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Services.DqtApi;

/// <summary>
/// A fake implementation of <see cref="IDqtApiClient"/> that writes data to a local JSON file.
/// </summary>
public class FakeDqtApiClient : IDqtApiClient
{
    private readonly object _gate = new object();
    private readonly TeacherIdentityServerDbContext _dbContext;

    public FakeDqtApiClient(TeacherIdentityServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TeacherInfo?> GetTeacherByTrn(string trn)
    {
        TeacherInfo? result = default;
        WithDatabaseFile(db => db.Teachers.TryGetValue(trn, out result));

        if (result is null)
        {
            var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Trn == trn);

            if (user is not null)
            {
                result = new TeacherInfo()
                {
                    Trn = trn,
                    FirstName = user.FirstName,
                    LastName = user.LastName
                };
            }
        }

        return result;
    }

    public void SetTeacherInfo(TeacherInfo teacherInfo)
    {
        WithDatabaseFile(db => db.Teachers[teacherInfo.Trn] = teacherInfo);
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
        public Dictionary<string, TeacherInfo> Teachers { get; init; } = new();
    }
}
