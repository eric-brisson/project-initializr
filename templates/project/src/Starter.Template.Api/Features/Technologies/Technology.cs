#if (UseEfCore)
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

#endif
namespace Starter.Template.Api.Features.Technologies;

// L'entité : ce qui est stocké pour chaque Technology. L'API ne la renvoie jamais directement : les tranches renvoient TechnologyDto.
public sealed class Technology
{
    public int Id { get; set; }

    public required string Name { get; set; }
    //#if (IsFeature)

    // TODO : ajoutez les autres propriétés de l'entité.
    //#endif
}
#if (UseEfCore)

// Comment EF Core stocke l'entité. AppDbContext trouve cette classe tout seul : aucun DbSet à ajouter.
#if (UseDatabase)
// La table elle-même est créée par les scripts Flyway de db/migrations : gardez les deux cohérents.
#else
// SQLite crée la table à partir de ce mapping au démarrage de l'API.
#endif
internal sealed class TechnologyConfiguration : IEntityTypeConfiguration<Technology>
{
    public void Configure(EntityTypeBuilder<Technology> builder)
    {
        builder.ToTable("Technologies");

#if (UseInMemory)
        // NOCASE : les noms se comparent sans tenir compte de la casse, comme avec la collation par défaut de SQL Server.
        builder.Property(item => item.Name).HasMaxLength(100).UseCollation("NOCASE");
#else
        builder.Property(item => item.Name).HasMaxLength(100);
#endif

        // Les noms sont uniques : enregistrer un doublon donne un 409 Conflict (UniqueConstraintExceptionHandler).
        builder.HasIndex(item => item.Name).IsUnique();
        //#if (IsFeature)

        // TODO : mappez les autres propriétés (longueurs, obligatoires, index...).
        //#endif
    }
}
#endif
