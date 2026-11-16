using Microsoft.EntityFrameworkCore;
using MusikalApi.Models;

namespace MusikalApi.Data;

public class MusikalDbContext : DbContext
{
    public MusikalDbContext(DbContextOptions<MusikalDbContext> options) : base(options)
    {
    }

    public DbSet<Marca> Marcas => Set<Marca>();
    public DbSet<TipoInstrumento> TiposInstrumento => Set<TipoInstrumento>();
    public DbSet<ModeloInstrumento> Modelos => Set<ModeloInstrumento>();
    public DbSet<Factura> Facturas => Set<Factura>();
    public DbSet<FacturaDetalle> FacturaDetalles => Set<FacturaDetalle>();
    public DbSet<ConfiguracionPasarela> ConfiguracionesPasarela => Set<ConfiguracionPasarela>();
    public DbSet<PayLinkPagoMock> PayLinkPagosMock => Set<PayLinkPagoMock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Relaciones Catálogo
        modelBuilder.Entity<ModeloInstrumento>()
            .HasOne(m => m.Marca)
            .WithMany(b => b.Modelos)
            .HasForeignKey(m => m.MarcaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ModeloInstrumento>()
            .HasOne(m => m.TipoInstrumento)
            .WithMany(t => t.Modelos)
            .HasForeignKey(m => m.TipoInstrumentoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relaciones Factura
        modelBuilder.Entity<Factura>()
            .HasIndex(f => f.CodigoPagoUnico)
            .IsUnique();

        modelBuilder.Entity<FacturaDetalle>()
            .HasOne(d => d.Factura)
            .WithMany(f => f.Detalles)
            .HasForeignKey(d => d.FacturaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FacturaDetalle>()
            .HasOne(d => d.ModeloInstrumento)
            .WithMany()
            .HasForeignKey(d => d.ModeloInstrumentoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
