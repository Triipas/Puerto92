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
        }
    }
}