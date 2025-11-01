using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Puerto92.Models;

namespace Puerto92.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<Usuario>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Aplicar migraciones pendientes
            await context.Database.MigrateAsync();

            string[] rolesNames = new[]
            {
                "Admin Maestro",
                "Administrador Local",
                "Supervisora de Calidad",
                "Contador",
                "Jefe de Cocina",
                "Mozo",
                "Cocinero",
                "Vajillero"
            };

            foreach (var roleName in rolesNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var role = new IdentityRole
                    {
                        Name = roleName,
                        NormalizedName = roleName.ToUpper()
                    };
                    await roleManager.CreateAsync(role);
                }
            }

            if (!await context.Locales.AnyAsync())
            {
                var localPrincipal = new Local
                {
                    Codigo = "LOC-001",
                    Nombre = "Puerto 92 - Principal",
                    Direccion = "Av. Principal 123",
                    Distrito = "Lima",
                    Telefono = "987654321",
                    Activo = true,
                    FechaCreacion = DateTime.Now
                };
                context.Locales.Add(localPrincipal);
                await context.SaveChangesAsync();
            }

            var adminEmail = "admin";
            var adminUser = await userManager.FindByNameAsync(adminEmail);

            if (adminUser == null)
            {
                var primerLocal = await context.Locales.FirstAsync();

                adminUser = new Usuario
                {
                    UserName = "admin",
                    NombreCompleto = "Administrador Maestro",
                    LocalId = primerLocal.Id,
                    EsPrimerIngreso = false,
                    Activo = true,
                    FechaCreacion = DateTime.Now,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin Maestro");
                    Console.WriteLine("✅ Usuario Admin Maestro creado exitosamente");
                    Console.WriteLine("   👤 Usuario: admin");
                    Console.WriteLine("   🔑 Contraseña: Admin123");
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    Console.WriteLine($"❌ Error al crear admin: {errors}");
                }
            }

            var usuarioEjemplo = await userManager.FindByNameAsync("mozo1");
            if (usuarioEjemplo == null)
            {
                var primerLocal = await context.Locales.FirstAsync();

                usuarioEjemplo = new Usuario
                {
                    UserName = "mozo1",
                    NombreCompleto = "Juan Mozo Ejemplo",
                    LocalId = primerLocal.Id,
                    EsPrimerIngreso = false,
                    Activo = true,
                    FechaCreacion = DateTime.Now,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(usuarioEjemplo, "Mozo123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(usuarioEjemplo, "Mozo");
                    Console.WriteLine("✅ Usuario Mozo creado: mozo1 / Mozo123");
                }
            }
        }
    }
}