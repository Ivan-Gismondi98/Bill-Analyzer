using BollettaAnalyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BollettaAnalyzer.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
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
