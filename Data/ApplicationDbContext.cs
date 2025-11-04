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
        }
    }
}