using Microsoft.EntityFrameworkCore;
using Respawn;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.TestCommon;

public class DbHelper
{
    private readonly string _connectionString;
    private Checkpoint _checkpoint;
    private readonly SemaphoreSlim _schemaLock = new(1, 1);
    private bool _haveResetSchema = false;

    public DbHelper(string connectionString)
    {
        _connectionString = connectionString;
        _checkpoint = CreateCheckpoint();
    }

    public async Task ClearData()
    {
        using var dbContext = new TeacherIdentityServerDbContext(_connectionString);
        await dbContext.Database.OpenConnectionAsync();
        await _checkpoint.Reset(dbContext.Database.GetDbConnection());
    }

    public async Task EnsureSchema()
    {
        await _schemaLock.WaitAsync();

        try
        {
            if (!_haveResetSchema)
            {
                await ResetSchema();
                _haveResetSchema = true;
            }
        }
        finally
        {
            _schemaLock.Release();
        }
    }

    public async Task ResetSchema()
    {
        using var dbContext = new TeacherIdentityServerDbContext(_connectionString);
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        _checkpoint = CreateCheckpoint();
    }

    private static Checkpoint CreateCheckpoint() => new Checkpoint()
    {
        DbAdapter = DbAdapter.Postgres
    };
}
