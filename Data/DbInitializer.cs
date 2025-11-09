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

            // ===== 2. CREAR LOCALES INICIALES =====
            if (!await context.Locales.AnyAsync())
            {
                // Local Corporativo (para roles administrativos)
                var localCorporativo = new Local
                {
                    Codigo = "CORP-00",
                    Nombre = "Corporativo",
                    Direccion = "Oficinas Centrales",
                    Distrito = "Lima",
                    Ciudad = "Lima",
                    Telefono = "987654321",
                    Activo = true,
                    FechaCreacion = DateTime.Now
                };
                context.Locales.Add(localCorporativo);

                // Local Principal (primer local operativo)
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
                // Obtener local corporativo
                var localCorporativo = await context.Locales.FirstOrDefaultAsync(l => l.Codigo == "CORP-00");
                if (localCorporativo == null)
                {
                    Console.WriteLine("❌ Error: No se encontró el local corporativo");
                    return;
                }

                adminUser = new Usuario
                {
                    UserName = "admin",
                    NombreCompleto = "Administrador Maestro",
                    LocalId = localCorporativo.Id,
                    EsPrimerIngreso = false, // Admin no necesita cambiar contraseña
                    PasswordReseteada = false,
                    Activo = true,
                    FechaCreacion = DateTime.Now,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin Maestro");
                    Console.WriteLine("✅ Usuario Admin Maestro creado exitosamente");
                    Console.WriteLine($"   👤 Usuario: admin");
                    Console.WriteLine($"   🔑 Contraseña: Admin123");
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    Console.WriteLine($"❌ Error al crear admin: {errors}");
                }
            }
/*
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
*/
            // ===== 5. USUARIOS DE PRUEBA POR ROL =====

            // Asegurar que existen los locales requeridos
            var localCorp = await context.Locales.FirstOrDefaultAsync(l => l.Codigo == "CORP-00");
            var localPuerto92 = await context.Locales.FirstOrDefaultAsync(l => l.Codigo == "LOC-001");

            if (localCorp == null || localPuerto92 == null)
            {
                Console.WriteLine("❌ Error: Faltan locales 'CORP-00' o 'LOC-001'. Verifica el seeding de Locales.");
                return;
            }

            // Credenciales de prueba sugeridas (puedes cambiarlas si deseas)
            await EnsureUserWithRole(
                userManager,
                roleManager,
                userName: "adminlocal1",
                nombreCompleto: "Admin Local Pruebas",
                roleName: "Administrador Local",
                localId: localPuerto92.Id,
                password: "Puerto92_Admin1"
            );

            await EnsureUserWithRole(
                userManager,
                roleManager,
                userName: "contador1",
                nombreCompleto: "Contador Pruebas",
                roleName: "Contador",
                localId: localCorp.Id,
                password: "Puerto92_Cont1"
            );

            await EnsureUserWithRole(
                userManager,
                roleManager,
                userName: "supervisora1",
                nombreCompleto: "Supervisora Calidad Pruebas",
                roleName: "Supervisora de Calidad",
                localId: localCorp.Id,
                password: "Puerto92_Sup1"
            );

            await EnsureUserWithRole(
                userManager,
                roleManager,
                userName: "mozo1",
                nombreCompleto: "Mozo Pruebas1",
                roleName: "Mozo",
                localId: localPuerto92.Id,
                password: "Puerto92_Moz1"
            );

            await EnsureUserWithRole(
                userManager,
                roleManager,
                userName: "mozo2",
                nombreCompleto: "Mozo Pruebas2",
                roleName: "Mozo",
                localId: localPuerto92.Id,
                password: "Puerto92_Moz2"
            );

            await EnsureUserWithRole(
                userManager,
                roleManager,
                userName: "cocinero1",
                nombreCompleto: "Cocinero Pruebas1",
                roleName: "Cocinero",
                localId: localPuerto92.Id,
                password: "Puerto92_Co1"
            );

            await EnsureUserWithRole(
                userManager,
                roleManager,
                userName: "cocinero2",
                nombreCompleto: "Cocinero Pruebas2",
                roleName: "Cocinero",
                localId: localPuerto92.Id,
                password: "Puerto92_Co2"
            );

            // ===== 6. CREAR CATEGORÍAS Y PRODUCTOS DE EJEMPLO =====

            // Verificar si ya existen categorías
            if (!await context.Categorias.AnyAsync())
            {
                Console.WriteLine("📦 Creando categorías de ejemplo...");

                var categorias = new List<Categoria>
                {
                    // Bebidas
                    new Categoria
                    {
                        Tipo = TipoCategoria.Bebidas,
                        Nombre = "Gaseosas",
                        Orden = 1,
                        Activo = true,
                        FechaCreacion = DateTime.Now,
                        CreadoPor = "Sistema"
                    },
                    new Categoria
                    {
                        Tipo = TipoCategoria.Bebidas,
                        Nombre = "Jugos",
                        Orden = 2,
                        Activo = true,
                        FechaCreacion = DateTime.Now,
                        CreadoPor = "Sistema"
                    },

                    // Cocina
                    new Categoria
                    {
                        Tipo = TipoCategoria.Cocina,
                        Nombre = "Abarrotes",
                        Orden = 1,
                        Activo = true,
                        FechaCreacion = DateTime.Now,
                        CreadoPor = "Sistema"
                    },
                    new Categoria
                    {
                        Tipo = TipoCategoria.Cocina,
                        Nombre = "Carnes",
                        Orden = 2,
                        Activo = true,
                        FechaCreacion = DateTime.Now,
                        CreadoPor = "Sistema"
                    },

                    // Utensilios
                    new Categoria
                    {
                        Tipo = TipoCategoria.Utensilios,
                        Nombre = "Cubiertos",
                        Orden = 1,
                        Activo = true,
                        FechaCreacion = DateTime.Now,
                        CreadoPor = "Sistema"
                    },
                    new Categoria
                    {
                        Tipo = TipoCategoria.Utensilios,
                        Nombre = "Vajilla",
                        Orden = 2,
                        Activo = true,
                        FechaCreacion = DateTime.Now,
                        CreadoPor = "Sistema"
                    }
                };

                context.Categorias.AddRange(categorias);
                await context.SaveChangesAsync();

                Console.WriteLine($"✅ {categorias.Count} categorías creadas exitosamente");

                // ===== CREAR PRODUCTOS ASOCIADOS A LAS CATEGORÍAS =====

                // Obtener las categorías recién creadas
                var catGaseosas = await context.Categorias.FirstOrDefaultAsync(c => c.Nombre == "Gaseosas");
                var catJugos = await context.Categorias.FirstOrDefaultAsync(c => c.Nombre == "Jugos");
                var catAbarrotes = await context.Categorias.FirstOrDefaultAsync(c => c.Nombre == "Abarrotes");
                var catCarnes = await context.Categorias.FirstOrDefaultAsync(c => c.Nombre == "Carnes");

                if (catGaseosas == null || catJugos == null || catAbarrotes == null || catCarnes == null)
                {
                    Console.WriteLine("⚠️ No se pudieron cargar las categorías para crear productos");
                }
                else
                {
                    Console.WriteLine("📦 Creando productos de ejemplo...");

                    var productos = new List<Producto>
                    {
                        // Productos de Bebidas - Gaseosas
                        new Producto
                        {
                            Codigo = "BEB-001",
                            Nombre = "Coca Cola 2L",
                            CategoriaId = catGaseosas.Id,
                            Unidad = "Unidad",
                            PrecioCompra = 4.50m,
                            PrecioVenta = 6.00m,
                            Descripcion = "Gaseosa Coca Cola 2 litros",
                            Activo = true,
                            FechaCreacion = DateTime.Now,
                            CreadoPor = "Sistema"
                        },

                        // Productos de Bebidas - Jugos
                        new Producto
                        {
                            Codigo = "BEB-002",
                            Nombre = "Jugo Naranja Gloria 1L",
                            CategoriaId = catJugos.Id,
                            Unidad = "Unidad",
                            PrecioCompra = 5.00m,
                            PrecioVenta = 7.00m,
                            Descripcion = "Jugo de naranja Gloria 1 litro",
                            Activo = true,
                            FechaCreacion = DateTime.Now,
                            CreadoPor = "Sistema"
                        },

                        // Productos de Cocina - Abarrotes
                        new Producto
                        {
                            Codigo = "COC-001",
                            Nombre = "Arroz Superior 1kg",
                            CategoriaId = catAbarrotes.Id,
                            Unidad = "Kilogramo",
                            PrecioCompra = 3.50m,
                            PrecioVenta = 4.50m,
                            Descripcion = "Arroz superior extra de 1 kilogramo",
                            Activo = true,
                            FechaCreacion = DateTime.Now,
                            CreadoPor = "Sistema"
                        },

                        // Productos de Cocina - Carnes
                        new Producto
                        {
                            Codigo = "COC-002",
                            Nombre = "Pollo Entero",
                            CategoriaId = catCarnes.Id,
                            Unidad = "Kilogramo",
                            PrecioCompra = 8.00m,
                            PrecioVenta = 12.00m,
                            Descripcion = "Pollo fresco entero por kilogramo",
                            Activo = true,
                            FechaCreacion = DateTime.Now,
                            CreadoPor = "Sistema"
                        }
                    };

                    context.Productos.AddRange(productos);
                    await context.SaveChangesAsync();

                    Console.WriteLine($"✅ {productos.Count} productos creados exitosamente");
                }

                // ===== CREAR UTENSILIOS ASOCIADOS A LAS CATEGORÍAS DE UTENSILIOS =====

                var catCubiertos = await context.Categorias.FirstOrDefaultAsync(c => c.Nombre == "Cubiertos");
                var catVajilla = await context.Categorias.FirstOrDefaultAsync(c => c.Nombre == "Vajilla");

                if (catCubiertos == null || catVajilla == null)
                {
                    Console.WriteLine("⚠️ No se pudieron cargar las categorías de utensilios");
                }
                else
                {
                    Console.WriteLine("🍴 Creando utensilios de ejemplo...");

                    var utensilios = new List<Utensilio>
                    {
                        // Cubiertos
                        new Utensilio
                        {
                            Codigo = "CUB-001",
                            Nombre = "Cuchara de Mesa",
                            Tipo = catCubiertos.Nombre,
                            Unidad = "Unidad",
                            Precio = 2.50m,
                            Descripcion = "Cuchara de acero inoxidable",
                            Activo = true,
                            FechaCreacion = DateTime.Now,
                            CreadoPor = "Sistema"
                        },

                        // Vajilla
                        new Utensilio
                        {
                            Codigo = "VAJ-001",
                            Nombre = "Plato Hondo",
                            Tipo = catVajilla.Nombre,
                            Unidad = "Unidad",
                            Precio = 5.00m,
                            Descripcion = "Plato hondo de cerámica blanca",
                            Activo = true,
                            FechaCreacion = DateTime.Now,
                            CreadoPor = "Sistema"
                        }
                    };

                    context.Utensilios.AddRange(utensilios);
                    await context.SaveChangesAsync();

                    Console.WriteLine($"✅ {utensilios.Count} utensilios creados exitosamente");
                }
            }
            else
            {
                Console.WriteLine("ℹ️ Las categorías ya existen, omitiendo seeding de categorías y productos");
            }
        
        }

        // Función local para evitar duplicar código
        private static async Task EnsureUserWithRole(UserManager<Usuario> userManager, RoleManager<IdentityRole> roleManager, string userName, string nombreCompleto, string roleName, int localId, string password)
        {
            // Si el rol no existiera por algún motivo, lo creamos
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole
                {
                    Name = roleName,
                    NormalizedName = roleName.ToUpper()
                });
            }

            var existing = await userManager.FindByNameAsync(userName);
            if (existing != null) return;

            var nuevo = new Usuario
            {
                UserName = userName,
                NombreCompleto = nombreCompleto,
                LocalId = localId,
                EsPrimerIngreso = false,
                PasswordReseteada = false,
                Activo = true,
                FechaCreacion = DateTime.Now,
                EmailConfirmed = true
            };

            // Usa contraseñas de prueba con complejidad suficiente
            var create = await userManager.CreateAsync(nuevo, password);
            if (create.Succeeded)
            {
                var addRole = await userManager.AddToRoleAsync(nuevo, roleName);
                if (addRole.Succeeded)
                {
                    Console.WriteLine($"✅ Usuario {roleName} creado: {userName} / {password}");
                }
                else
                {
                    var errors = string.Join(", ", addRole.Errors.Select(e => e.Description));
                    Console.WriteLine($"❌ Error al asignar rol a {userName}: {errors}");
                }
            }
            else
            {
                var errors = string.Join(", ", create.Errors.Select(e => e.Description));
                Console.WriteLine($"❌ Error al crear {userName}: {errors}");
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