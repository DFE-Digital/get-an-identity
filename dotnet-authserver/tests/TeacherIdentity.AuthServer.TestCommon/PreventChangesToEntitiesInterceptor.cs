using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace TeacherIdentity.AuthServer.TestCommon;

public class PreventChangesToEntitiesInterceptor<TEntity, TId> : SaveChangesInterceptor where TEntity : class
{
    private readonly IEnumerable<TId> _entityIds;
    private readonly Func<TEntity, TId> _idAccessor;

    public PreventChangesToEntitiesInterceptor(IEnumerable<TId> entityIds, Func<TEntity, TId> idAccessor)
    {
        _entityIds = entityIds;
        _idAccessor = idAccessor;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        throw new NotImplementedException();
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        var entities = eventData.Context!.ChangeTracker.Entries<TEntity>();

        foreach (var entity in entities)
        {
            if ((entity.State == EntityState.Modified
                || entity.State == EntityState.Deleted)
                && _entityIds.Contains(_idAccessor(entity.Entity)))
            {
                throw new InvalidOperationException("Existing entity cannot be modified or deleted, consider using new entities within tests.");
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
