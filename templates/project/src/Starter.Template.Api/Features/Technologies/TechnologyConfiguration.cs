using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Starter.Template.Api.Features.Technologies;

// Picked up by AppDbContext (ApplyConfigurationsFromAssembly): no DbSet needed, the slices use db.Set<Technology>().
#if (UseDatabase)
// Must match the Technologies table of the Flyway scripts in db/migrations.
#else
// SQLite creates the Technologies table from this mapping when the API starts.
#endif
internal sealed class TechnologyConfiguration : IEntityTypeConfiguration<Technology>
{
    public void Configure(EntityTypeBuilder<Technology> builder)
    {
        builder.ToTable("Technologies");

#if (UseInMemory)
        // NOCASE: names compare case-insensitively, as with the default collation of SQL Server.
        builder.Property(item => item.Name).HasMaxLength(100).UseCollation("NOCASE");
#else
        builder.Property(item => item.Name).HasMaxLength(100);
#endif

        // Saving a duplicate name gets a 409 Conflict (UniqueConstraintExceptionHandler). Remove it if names may repeat.
        builder.HasIndex(item => item.Name).IsUnique();
        //#if (IsFeature)

        // TODO: map the other properties (lengths, required, indexes...).
        //#endif
    }
}
