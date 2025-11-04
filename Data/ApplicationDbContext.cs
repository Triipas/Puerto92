using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Puerto92.Models;

namespace Puerto92.Data
{
    public class ApplicationDbContext : IdentityDbContext<Usuario>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Local> Locales { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<AsignacionKardex> AsignacionesKardex { get; set; }
        public DbSet<Utensilio> Utensilios { get; set; } // ⭐ NUEVO

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Usuario>()
                .HasOne(u => u.Local)
                .WithMany(l => l.Usuarios)
                .HasForeignKey(u => u.LocalId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Local>()
                .HasIndex(l => l.Codigo)
                .IsUnique();

            // Índices para AuditLogs para mejorar el rendimiento de consultas
            builder.Entity<AuditLog>()
                .HasIndex(a => a.FechaHora);
            builder.Entity<AuditLog>()
                .HasIndex(a => a.UsuarioAccion);
            builder.Entity<AuditLog>()
                .HasIndex(a => a.Accion);
            builder.Entity<AuditLog>()
                .HasIndex(a => a.Modulo);

            builder.Entity<Categoria>(entity =>
            {
                entity.ToTable("Categorias", t =>
                {
                    t.HasCheckConstraint("CK_Categorias_Orden_Rango", "[Orden] BETWEEN 1 AND 999");
                });

                entity.Property(c => c.Tipo).HasMaxLength(20).IsRequired();
                entity.Property(c => c.Nombre).HasMaxLength(100).IsRequired();
                entity.HasIndex(c => new { c.Tipo, c.Nombre }).IsUnique();
                entity.HasIndex(c => new { c.Tipo, c.Orden }).IsUnique();
                entity.HasIndex(c => new { c.Tipo, c.Activo, c.Orden });

                entity.Property(c => c.Activo).HasDefaultValue(true);
                entity.Property(c => c.FechaCreacion).HasDefaultValueSql("getdate()");
                entity.Property<byte[]>("RowVersion").IsRowVersion();
            });

            // Configuración de AsignacionKardex
            builder.Entity<AsignacionKardex>(entity =>
            {
                entity.HasOne(a => a.Empleado)
                    .WithMany()
                    .HasForeignKey(a => a.EmpleadoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Local)
                    .WithMany()
                    .HasForeignKey(a => a.LocalId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(a => new { a.LocalId, a.Fecha, a.TipoKardex });
                entity.HasIndex(a => new { a.EmpleadoId, a.Fecha });
                entity.HasIndex(a => a.Estado);
                entity.HasIndex(a => a.FechaCreacion);

                entity.Property(a => a.Estado).HasDefaultValue("Pendiente");
                entity.Property(a => a.FechaCreacion).HasDefaultValueSql("getdate()");
                entity.Property(a => a.EsReasignacion).HasDefaultValue(false);
                entity.Property(a => a.RegistroIniciado).HasDefaultValue(false);
                entity.Property(a => a.NotificacionEnviada).HasDefaultValue(false);
            });

            // ⭐ NUEVO: Configuración de Utensilios
            builder.Entity<Utensilio>(entity =>
            {
                entity.ToTable("Utensilios");

                entity.Property(u => u.Codigo)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(u => u.Nombre)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(u => u.Tipo)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(u => u.Unidad)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(u => u.Precio)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(u => u.Descripcion)
                    .HasMaxLength(500);

                entity.Property(u => u.Activo)
                    .HasDefaultValue(true);

                entity.Property(u => u.FechaCreacion)
                    .HasDefaultValueSql("getdate()");

                entity.Property(u => u.CreadoPor)
                    .HasMaxLength(100);

                entity.Property(u => u.ModificadoPor)
                    .HasMaxLength(100);

                // Índices para optimizar consultas
                entity.HasIndex(u => u.Codigo)
                    .IsUnique();

                entity.HasIndex(u => new { u.Tipo, u.Activo });

                entity.HasIndex(u => u.Nombre);
            });
        }
    }
}