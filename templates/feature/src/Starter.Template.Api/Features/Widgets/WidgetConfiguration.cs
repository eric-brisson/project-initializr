using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Starter.Template.Api.Features.Widgets;

// Picked up by AppDbContext (ApplyConfigurationsFromAssembly): no DbSet needed.
// With SQL Server, the Widgets table must match: create it in a new Flyway script.
internal sealed class WidgetConfiguration : IEntityTypeConfiguration<Widget>
{
    public void Configure(EntityTypeBuilder<Widget> builder)
    {
        builder.ToTable("Widgets");

        builder.Property(item => item.Name).HasMaxLength(100);

        // TODO: map the other properties (lengths, required, indexes...).
    }
}
