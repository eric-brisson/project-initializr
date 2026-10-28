using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Starter.Template.Api.Features.Technologies;

#if (UseDatabase)
// Must match the Technologies table created by db/migrations/V1__create_technologies.sql.
#else
// SQLite creates the Technologies table from this mapping when the API starts.
#endif
internal sealed class TechnologyConfiguration : IEntityTypeConfiguration<Technology>
{
    public void Configure(EntityTypeBuilder<Technology> builder)
    {
        builder.ToTable("Technologies");

        builder.Property(technology => technology.Name).HasMaxLength(100);
        builder.Property(technology => technology.Version).HasMaxLength(50);

        builder.HasIndex(technology => technology.Name).IsUnique();
    }
}
