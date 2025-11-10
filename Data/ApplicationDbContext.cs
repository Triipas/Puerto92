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
        public DbSet<Utensilio> Utensilios { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Notificacion> Notificaciones { get; set; }
        public DbSet<KardexBebidas> KardexBebidas { get; set; }
        public DbSet<KardexBebidasDetalle> KardexBebidasDetalles { get; set; }
        public DbSet<PersonalPresente> PersonalPresente { get; set; }


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

            // Índices para AuditLogs
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

            builder.Entity<Utensilio>(entity =>
{
    entity.ToTable("Utensilios");

    entity.Property(u => u.Codigo)
        .HasMaxLength(20)
        .IsRequired();

    entity.Property(u => u.Nombre)
        .HasMaxLength(100)
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

    // ⭐ NUEVA: Relación con Categoría
    entity.HasOne(u => u.Categoria)
        .WithMany()
        .HasForeignKey(u => u.CategoriaId)
        .OnDelete(DeleteBehavior.Restrict);

    entity.HasIndex(u => u.Codigo)
        .IsUnique();

    entity.HasIndex(u => new { u.CategoriaId, u.Activo });

    entity.HasIndex(u => u.Nombre);
});

            // ⭐ NUEVO: Configuración de Productos
            builder.Entity<Producto>(entity =>
            {
                entity.ToTable("Productos");

                entity.Property(p => p.Codigo)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(p => p.Nombre)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(p => p.Unidad)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(p => p.PrecioCompra)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(p => p.PrecioVenta)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(p => p.Descripcion)
                    .HasMaxLength(500);

                entity.Property(p => p.Activo)
                    .HasDefaultValue(true);

                entity.Property(p => p.FechaCreacion)
                    .HasDefaultValueSql("getdate()");

                entity.Property(p => p.CreadoPor)
                    .HasMaxLength(100);

                entity.Property(p => p.ModificadoPor)
                    .HasMaxLength(100);

                // Relación con Categoría
                entity.HasOne(p => p.Categoria)
                    .WithMany()
                    .HasForeignKey(p => p.CategoriaId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Índices para optimizar consultas
                entity.HasIndex(p => p.Codigo)
                    .IsUnique();

                entity.HasIndex(p => new { p.CategoriaId, p.Activo });

                entity.HasIndex(p => p.Nombre);
            });

            builder.Entity<Proveedor>(entity =>
            {
                entity.ToTable("Proveedores");

                entity.Property(p => p.RUC)
                    .HasMaxLength(11)
                    .IsRequired();

                entity.Property(p => p.Nombre)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(p => p.Categoria)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(p => p.Telefono)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(p => p.Email)
                    .HasMaxLength(100);

                entity.Property(p => p.PersonaContacto)
                    .HasMaxLength(100);

                entity.Property(p => p.Direccion)
                    .HasMaxLength(300);

                entity.Property(p => p.Activo)
                    .HasDefaultValue(true);

                entity.Property(p => p.FechaCreacion)
                    .HasDefaultValueSql("getdate()");

                entity.Property(p => p.CreadoPor)
                    .HasMaxLength(100);

                entity.Property(p => p.ModificadoPor)
                    .HasMaxLength(100);

                // Índice único para RUC
                entity.HasIndex(p => p.RUC)
                    .IsUnique();

                // Índices para mejorar búsquedas
                entity.HasIndex(p => new { p.Categoria, p.Activo });

                entity.HasIndex(p => p.Nombre);
            });

            // Configuración de Notificaciones
            builder.Entity<Notificacion>(entity =>
            {
                entity.ToTable("Notificaciones");

                entity.HasOne(n => n.Usuario)
                    .WithMany()
                    .HasForeignKey(n => n.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(n => new { n.UsuarioId, n.Leida, n.FechaCreacion });
                entity.HasIndex(n => n.FechaExpiracion);
                entity.HasIndex(n => n.Tipo);

                entity.Property(n => n.FechaCreacion)
                    .HasDefaultValueSql("getdate()");
            });

            // Configuración de Kardex de Bebidas
            builder.Entity<KardexBebidas>(entity =>
            {
                entity.HasOne(k => k.Asignacion)
                    .WithMany()
                    .HasForeignKey(k => k.AsignacionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(k => k.Local)
                    .WithMany()
                    .HasForeignKey(k => k.LocalId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(k => k.Empleado)
                    .WithMany()
                    .HasForeignKey(k => k.EmpleadoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(k => new { k.LocalId, k.Fecha, k.Estado });
                entity.HasIndex(k => k.AsignacionId).IsUnique();
            });

            builder.Entity<KardexBebidasDetalle>(entity =>
            {
                entity.HasOne(d => d.KardexBebidas)
                    .WithMany(k => k.Detalles)
                    .HasForeignKey(d => d.KardexBebidasId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Producto)
                    .WithMany()
                    .HasForeignKey(d => d.ProductoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(d => new { d.KardexBebidasId, d.Orden });
            });

            builder.Entity<PersonalPresente>(entity =>
            {
                entity.ToTable("PersonalPresente");

                entity.HasOne(p => p.Empleado)
                    .WithMany()
                    .HasForeignKey(p => p.EmpleadoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(p => new { p.KardexId, p.TipoKardex, p.EmpleadoId })
                    .IsUnique();

                entity.HasIndex(p => new { p.KardexId, p.TipoKardex });

                entity.Property(p => p.FechaRegistro)
                    .HasDefaultValueSql("getdate()");
            });

        }
    }
}