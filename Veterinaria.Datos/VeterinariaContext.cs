using Microsoft.EntityFrameworkCore;
using Veterinaria.Entidades.Models;

namespace Veterinaria.Datos;

public partial class VeterinariaContext : DbContext
{
    public VeterinariaContext(DbContextOptions<VeterinariaContext> options) : base(options) { }

    public virtual DbSet<Cita> Citas { get; set; }
    public virtual DbSet<Mascota> Mascotas { get; set; }
    public virtual DbSet<Usuario> Usuarios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cita>(entity =>
        {
            entity.ToTable("Cita");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Costo).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Estado).HasMaxLength(20).HasDefaultValue("Pendiente");
            entity.Property(e => e.Motivo).HasMaxLength(200);

            entity.HasOne(d => d.Mascota).WithMany(p => p.Citas)
                .HasForeignKey(d => d.MascotaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Cita_Mascota");
        });

        modelBuilder.Entity<Mascota>(entity =>
        {
            entity.ToTable("Mascota");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Especie).HasMaxLength(30);
            entity.Property(e => e.Nombre).HasMaxLength(60);
            entity.Property(e => e.NombreDueno).HasMaxLength(100);
            entity.Property(e => e.Raza).HasMaxLength(60);
            entity.Property(e => e.Telefono).HasMaxLength(20);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuario");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Correo).IsUnique();
            entity.Property(e => e.Correo).HasMaxLength(120);
            entity.Property(e => e.ClaveHash).HasMaxLength(100);
            entity.Property(e => e.Rol).HasMaxLength(30);
        });
    }
}
