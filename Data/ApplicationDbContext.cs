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
        public DbSet<Utensilio> Utensilios { get; set; }
        public DbSet<Producto> Productos { get; set; } = null!;
          public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Categoria> Categorias { get; set; }

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
                    // Check constraint para alinear con [Range(1,999)] del ViewModel
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
            
        }
    }
}