using BollettaAnalyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BollettaAnalyzer.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    // PostgreSQL (Npgsql) accetta su "timestamp with time zone" SOLO date con
    // Kind=Utc: una DateTime "Unspecified" (es. new DateTime(2026,3,1), o le date
    // estratte dall'OCR) farebbe fallire l'INSERT. Questi converter normalizzano
    // OGNI data del modello a UTC in scrittura e la rileggono marcata UTC,
    // qualunque sia il provider (per SQLite è un no-op innocuo).
    private static readonly ValueConverter<DateTime, DateTime> UtcConverter = new(
        v => v.Kind == DateTimeKind.Utc ? v
           : v.Kind == DateTimeKind.Local ? v.ToUniversalTime()
           : DateTime.SpecifyKind(v, DateTimeKind.Utc),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    private static readonly ValueConverter<DateTime?, DateTime?> UtcNullableConverter = new(
        v => v == null ? null
           : v.Value.Kind == DateTimeKind.Utc ? v
           : v.Value.Kind == DateTimeKind.Local ? v.Value.ToUniversalTime()
           : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc),
        v => v == null ? null : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc));

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Utente> Utenti => Set<Utente>();
    public DbSet<Contratto> Contratti => Set<Contratto>();
    public DbSet<Bolletta> Bollette => Set<Bolletta>();
    public DbSet<VoceDiCosto> VociDiCosto => Set<VoceDiCosto>();
    public DbSet<LetturaContatore> Letture => Set<LetturaContatore>();
    public DbSet<DispositivoElettronico> Dispositivi => Set<DispositivoElettronico>();
    public DbSet<DocumentoContratto> DocumentiContratto => Set<DocumentoContratto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Applica la normalizzazione UTC a tutte le proprietà DateTime del modello.
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        foreach (var property in entity.GetProperties())
        {
            if (property.ClrType == typeof(DateTime))
                property.SetValueConverter(UtcConverter);
            else if (property.ClrType == typeof(DateTime?))
                property.SetValueConverter(UtcNullableConverter);
        }
    }

    public override int SaveChanges()
    {
        StampMetadati();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampMetadati();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampMetadati()
    {
        foreach (var entry in ChangeTracker.Entries<Domain.Common.BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
