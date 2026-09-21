using Microsoft.EntityFrameworkCore;
using SistemaTanqueReactor.Models;

namespace SistemaTanqueReactor.Data;

public class TanqueReactorContext : DbContext
{
    public TanqueReactorContext(DbContextOptions<TanqueReactorContext> options) : base(options)
    {
    }

    public DbSet<Turno> Turnos => Set<Turno>();
    public DbSet<Operario> Operarios => Set<Operario>();
    public DbSet<Insumo> Insumos => Set<Insumo>();
    public DbSet<Lote> Lotes => Set<Lote>();
    public DbSet<CargaReactor> CargasReactor => Set<CargaReactor>();
    public DbSet<CargaInsumo> CargaInsumos => Set<CargaInsumo>();
    public DbSet<ControlTemperatura> ControlesTemperatura => Set<ControlTemperatura>();
    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();
    public DbSet<ChecklistResultado> ChecklistResultados => Set<ChecklistResultado>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Turno>(e =>
        {
            e.ToTable("Turnos");
            e.HasKey(x => x.IdTurno);
            e.Property(x => x.Nombre).HasMaxLength(30).IsRequired();
        });

        modelBuilder.Entity<Operario>(e =>
        {
            e.ToTable("Operarios");
            e.HasKey(x => x.IdOperario);
            e.Property(x => x.Nombres).HasMaxLength(100).IsRequired();
            e.Property(x => x.Apellidos).HasMaxLength(100);
            e.Property(x => x.Dni).HasMaxLength(15);
        });

        modelBuilder.Entity<Insumo>(e =>
        {
            e.ToTable("Insumos");
            e.HasKey(x => x.IdInsumo);
            e.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
            e.Property(x => x.Unidad).HasMaxLength(20);
        });

        modelBuilder.Entity<Lote>(e =>
        {
            e.ToTable("Lotes");
            e.HasKey(x => x.IdLote);
            e.Property(x => x.NumeroLote).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.NumeroLote).IsUnique();
            e.HasOne(x => x.Insumo).WithMany(i => i.Lotes).HasForeignKey(x => x.IdInsumo);
        });

        modelBuilder.Entity<CargaReactor>(e =>
        {
            e.ToTable("CargasReactor");
            e.HasKey(x => x.IdCarga);
            e.Property(x => x.Estado).HasMaxLength(20);
            e.Property(x => x.LoteProductoFinal).HasMaxLength(50);
            e.Property(x => x.TiempoSecadoAlVacio).HasMaxLength(30);
            e.Property(x => x.Observaciones).HasMaxLength(500);
            e.Property(x => x.UsuarioRegistro).HasMaxLength(80);
            e.Property(x => x.UsuarioModificacion).HasMaxLength(80);

            e.HasOne(x => x.Turno).WithMany(t => t.Cargas).HasForeignKey(x => x.IdTurno);
            e.HasOne(x => x.Operario).WithMany(o => o.Cargas).HasForeignKey(x => x.IdOperario);
        });

        modelBuilder.Entity<CargaInsumo>(e =>
        {
            e.ToTable("CargaInsumos");
            e.HasKey(x => x.IdCargaInsumo);
            e.HasOne(x => x.Carga).WithMany(c => c.Insumos).HasForeignKey(x => x.IdCarga).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Insumo).WithMany(i => i.CargaInsumos).HasForeignKey(x => x.IdInsumo);
            e.HasOne(x => x.Lote).WithMany(l => l.CargaInsumos).HasForeignKey(x => x.IdLote);
        });

        modelBuilder.Entity<ControlTemperatura>(e =>
        {
            e.ToTable("ControlesTemperatura");
            e.HasKey(x => x.IdControl);
            e.HasOne(x => x.Carga).WithMany(c => c.ControlesTemperatura).HasForeignKey(x => x.IdCarga).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChecklistItem>(e =>
        {
            e.ToTable("ChecklistItems");
            e.HasKey(x => x.IdItem);
            e.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
            e.Property(x => x.Categoria).HasMaxLength(50);
        });

        modelBuilder.Entity<ChecklistResultado>(e =>
        {
            e.ToTable("ChecklistResultados");
            e.HasKey(x => x.IdResultado);
            e.Property(x => x.Estado).HasMaxLength(20);
            e.HasOne(x => x.Carga).WithMany(c => c.ChecklistResultados).HasForeignKey(x => x.IdCarga).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Item).WithMany(i => i.Resultados).HasForeignKey(x => x.IdItem);
        });
    }
}
