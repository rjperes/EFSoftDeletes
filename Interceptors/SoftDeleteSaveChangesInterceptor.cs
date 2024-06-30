using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EFSoftDeletes.Interceptors
{
    public class SoftDeleteSaveChangesInterceptor : SaveChangesInterceptor
    {
        private const string _isDeletedProperty = Constants.IsDeletedProperty;

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            ConvertEntries(eventData.Context!);
            return base.SavingChanges(eventData, result);
        }

        private void ConvertEntries(DbContext context)
        {
            IEnumerable<EntityEntry> ownedEntries = null;

            foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>().Where(e => e.State == EntityState.Deleted))
            {
                entry.Property(_isDeletedProperty).CurrentValue = true;
                entry.State = EntityState.Modified;

                ownedEntries ??= context.ChangeTracker.Entries().Where(x => x.State == EntityState.Deleted && x.Metadata.IsOwned());

                foreach (var ownedEntry in ownedEntries)
                {
                    if (ownedEntry.Metadata.IsInOwnershipPath(entry.Metadata))
                    {
                        ownedEntry.State = EntityState.Modified;
                    }
                }
            }
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            ConvertEntries(eventData.Context!);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
