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

            // ===== 1. CREAR ROLES =====
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

            // ===== 2. CREAR LOCAL INICIAL =====
            if (!await context.Locales.AnyAsync())
            {
                var localPrincipal = new Local
                {
                    Codigo = "LOC-001",
                    Nombre = "Puerto 92 - Principal",
                    Direccion = "Av. Principal 123",
                    Distrito = "Lima",
                    Ciudad = "Lima",
                    Telefono = "987654321",
                    Activo = true,
                    FechaCreacion = DateTime.Now
                };
                context.Locales.Add(localPrincipal);
                await context.SaveChangesAsync();
            }

            // ===== 3. CREAR USUARIO ADMIN MAESTRO =====
            var adminEmail = "admin";
            var adminUser = await userManager.FindByNameAsync(adminEmail);

            if (adminUser == null)
            {
                var primerLocal = await context.Locales.FirstAsync();

                // Generar contraseña usando el mismo algoritmo
                string adminPassword = GenerateTemporaryPassword();

                adminUser = new Usuario
                {
                    UserName = "admin",
                    NombreCompleto = "Administrador Maestro",
                    LocalId = primerLocal.Id,
                    EsPrimerIngreso = false, // Admin no necesita cambiar contraseña
                    PasswordReseteada = false,
                    Activo = true,
                    FechaCreacion = DateTime.Now,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin Maestro");
                    Console.WriteLine("✅ Usuario Admin Maestro creado exitosamente");
                    Console.WriteLine($"   👤 Usuario: admin");
                    Console.WriteLine($"   🔑 Contraseña: {adminPassword}");
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    Console.WriteLine($"❌ Error al crear admin: {errors}");
                }
            }

            // ===== 4. CREAR USUARIO DE EJEMPLO (Opcional) =====
            var usuarioEjemplo = await userManager.FindByNameAsync("mozo1");
            if (usuarioEjemplo == null)
            {
                var primerLocal = await context.Locales.FirstAsync();
                
                // Generar contraseña usando el mismo algoritmo
                string mozoPassword = GenerateTemporaryPassword();

                usuarioEjemplo = new Usuario
                {
                    UserName = "mozo1",
                    NombreCompleto = "Juan Mozo Ejemplo",
                    LocalId = primerLocal.Id,
                    EsPrimerIngreso = false,
                    PasswordReseteada = false,
                    Activo = true,
                    FechaCreacion = DateTime.Now,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(usuarioEjemplo, mozoPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(usuarioEjemplo, "Mozo");
                    Console.WriteLine($"✅ Usuario Mozo creado: mozo1 / {mozoPassword}");
                }
            }
        }

        /// <summary>
        /// Genera una contraseña temporal segura usando el mismo algoritmo que el sistema
        /// </summary>
        private static string GenerateTemporaryPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
            var random = new Random();
            var password = "Puerto92_";
            
            for (int i = 0; i < 8; i++)
            {
                password += chars[random.Next(chars.Length)];
            }
            
            return password;
        }
    }
}