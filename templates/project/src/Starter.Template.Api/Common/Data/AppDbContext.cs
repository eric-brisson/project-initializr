using Microsoft.EntityFrameworkCore;

namespace Starter.Template.Api.Common.Data;

// One context for reads and writes. Read handlers project to DTOs (or use AsNoTracking), so nothing is tracked.
// Consider a separate read-only context once reads go to a read replica.
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Each feature maps its own entities, in IEntityTypeConfiguration classes next to them, and reaches them
    // with Set<TEntity>(): the context doesn't need to know the features.
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
