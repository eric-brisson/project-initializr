using Microsoft.EntityFrameworkCore;
using Starter.Template.Api.Features.Technologies;

namespace Starter.Template.Api.Common.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Technology> Technologies => Set<Technology>();

    // Each feature maps its own entities, in IEntityTypeConfiguration classes next to them.
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
