using Microsoft.EntityFrameworkCore;

namespace Starter.Template.Api.Common.Data;

// Un seul contexte pour les lectures et les écritures. Les handlers de lecture utilisent AsNoTracking : rien n'est suivi.
// Envisagez un contexte en lecture seule séparé le jour où les lectures iront vers un réplica.
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Chaque feature mappe ses propres entités, dans des classes IEntityTypeConfiguration à côté d'elles, et y accède
    // avec Set<TEntity>() : le contexte n'a pas besoin de connaître les features.
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
