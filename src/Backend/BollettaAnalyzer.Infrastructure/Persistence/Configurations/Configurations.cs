using BollettaAnalyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BollettaAnalyzer.Infrastructure.Persistence.Configurations;

public class UtenteConfig : IEntityTypeConfiguration<Utente>
{
    public void Configure(EntityTypeBuilder<Utente> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Email).IsRequired().HasMaxLength(256);
        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.Nome).HasMaxLength(120);
        b.Property(x => x.Cognome).HasMaxLength(120);

        b.HasMany(x => x.Contratti).WithOne(c => c.Utente).HasForeignKey(c => c.UtenteId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Dispositivi).WithOne(d => d.Utente).HasForeignKey(d => d.UtenteId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ContrattoConfig : IEntityTypeConfiguration<Contratto>
{
    public void Configure(EntityTypeBuilder<Contratto> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Fornitore).IsRequired().HasMaxLength(160);
        b.Property(x => x.NomeOfferta).HasMaxLength(160);
        DecimalCols(b, x => x.PotenzaImpegnataKw, x => x.PrezzoKwhMonorario, x => x.PrezzoKwhF1,
            x => x.PrezzoKwhF2, x => x.PrezzoKwhF3, x => x.PrezzoSm3, x => x.QuotaFissaMensile, x => x.CoefficienteConversione);

        b.HasMany(x => x.Bollette).WithOne(x => x.Contratto).HasForeignKey(x => x.ContrattoId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Letture).WithOne(x => x.Contratto).HasForeignKey(x => x.ContrattoId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Documento).WithOne(d => d.Contratto).HasForeignKey<DocumentoContratto>(d => d.ContrattoId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void DecimalCols(EntityTypeBuilder<Contratto> b,
        params System.Linq.Expressions.Expression<Func<Contratto, decimal>>[] props)
    {
        foreach (var p in props) b.Property(p).HasPrecision(18, 5);
    }
}

public class BollettaConfig : IEntityTypeConfiguration<Bolletta>
{
    public void Configure(EntityTypeBuilder<Bolletta> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.ImportoTotale).HasPrecision(18, 2);
        foreach (var name in new[] { nameof(Bolletta.ConsumoTotaleKwh), nameof(Bolletta.ConsumoF1Kwh),
            nameof(Bolletta.ConsumoF2Kwh), nameof(Bolletta.ConsumoF3Kwh), nameof(Bolletta.ConsumoSm3) })
            b.Property(name).HasColumnType("decimal(18,3)");

        b.HasMany(x => x.VociDiCosto).WithOne(v => v.Bolletta).HasForeignKey(v => v.BollettaId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class VoceDiCostoConfig : IEntityTypeConfiguration<VoceDiCosto>
{
    public void Configure(EntityTypeBuilder<VoceDiCosto> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Descrizione).HasMaxLength(200);
        b.Property(x => x.Importo).HasPrecision(18, 2);
    }
}

public class LetturaConfig : IEntityTypeConfiguration<LetturaContatore>
{
    public void Configure(EntityTypeBuilder<LetturaContatore> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.ValoreTotale).HasPrecision(18, 3);
        b.Property(x => x.ValoreF1).HasPrecision(18, 3);
        b.Property(x => x.ValoreF2).HasPrecision(18, 3);
        b.Property(x => x.ValoreF3).HasPrecision(18, 3);
    }
}

public class DocumentoContrattoConfig : IEntityTypeConfiguration<DocumentoContratto>
{
    public void Configure(EntityTypeBuilder<DocumentoContratto> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.ContrattoId).IsUnique();
        b.Property(x => x.NomeFile).HasMaxLength(260);
        b.Property(x => x.ContentType).HasMaxLength(120);
    }
}

public class DispositivoConfig : IEntityTypeConfiguration<DispositivoElettronico>
{
    public void Configure(EntityTypeBuilder<DispositivoElettronico> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).IsRequired().HasMaxLength(120);
        b.Property(x => x.OreUtilizzoGiornaliere).HasPrecision(6, 2);
        // Proprietà calcolate: non mappate a colonne.
        b.Ignore(x => x.ConsumoGiornalieroKwh);
        b.Ignore(x => x.ConsumoMensileKwh);
    }
}
