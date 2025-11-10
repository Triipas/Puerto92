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

            Console.WriteLine("🚀 Iniciando inicialización de base de datos...");

            // Aplicar migraciones pendientes
            try
            {
                Console.WriteLine("📊 Aplicando migraciones pendientes...");
                await context.Database.MigrateAsync();
                Console.WriteLine("✅ Migraciones aplicadas correctamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al aplicar migraciones: {ex.Message}");
                return;
            }

            // ===== 1. CREAR ROLES =====
            try
            {
                Console.WriteLine("👥 Creando roles del sistema...");
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
                        Console.WriteLine($"   ✓ Rol creado: {roleName}");
                    }
                }
                Console.WriteLine($"✅ {rolesNames.Length} roles verificados/creados");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al crear roles: {ex.Message}");
            }

            // ===== 2. CREAR LOCALES INICIALES =====
            try
            {
                Console.WriteLine("🏢 Verificando locales...");
                if (!await context.Locales.AnyAsync())
                {
                    Console.WriteLine("   Creando locales iniciales...");
                    
                    // Local Corporativo
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

                    // Local Principal
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
                    Console.WriteLine("✅ 2 locales creados correctamente");
                }
                else
                {
                    Console.WriteLine("ℹ️ Los locales ya existen");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al crear locales: {ex.Message}");
            }

            // ===== 3. CREAR USUARIO ADMIN MAESTRO =====
            try
            {
                Console.WriteLine("👤 Verificando usuario Admin Maestro...");
                var adminEmail = "admin";
                var adminUser = await userManager.FindByNameAsync(adminEmail);

                if (adminUser == null)
                {
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
                        EsPrimerIngreso = false,
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
                else
                {
                    Console.WriteLine("ℹ️ Usuario Admin ya existe");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al crear usuario admin: {ex.Message}");
            }

            // ===== 4. USUARIOS DE PRUEBA POR ROL =====
            try
            {
                Console.WriteLine("👥 Creando usuarios de prueba...");
                
                var localCorp = await context.Locales.FirstOrDefaultAsync(l => l.Codigo == "CORP-00");
                var localPuerto92 = await context.Locales.FirstOrDefaultAsync(l => l.Codigo == "LOC-001");

                if (localCorp == null || localPuerto92 == null)
                {
                    Console.WriteLine("❌ Error: Faltan locales requeridos");
                    return;
                }

                await EnsureUserWithRole(userManager, roleManager, "adminlocal1", "Admin Local Pruebas", "Administrador Local", localPuerto92.Id, "Puerto92_Admin1");
                await EnsureUserWithRole(userManager, roleManager, "contador1", "Contador Pruebas", "Contador", localCorp.Id, "Puerto92_Cont1");
                await EnsureUserWithRole(userManager, roleManager, "supervisora1", "Supervisora Calidad Pruebas", "Supervisora de Calidad", localCorp.Id, "Puerto92_Sup1");
                await EnsureUserWithRole(userManager, roleManager, "mozo1", "Mozo Pruebas1", "Mozo", localPuerto92.Id, "Puerto92_Moz1");
                await EnsureUserWithRole(userManager, roleManager, "mozo2", "Mozo Pruebas2", "Mozo", localPuerto92.Id, "Puerto92_Moz2");
                await EnsureUserWithRole(userManager, roleManager, "cocinero1", "Cocinero Pruebas1", "Cocinero", localPuerto92.Id, "Puerto92_Co1");
                await EnsureUserWithRole(userManager, roleManager, "cocinero2", "Cocinero Pruebas2", "Cocinero", localPuerto92.Id, "Puerto92_Co2");

                Console.WriteLine("✅ Usuarios de prueba creados/verificados");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al crear usuarios de prueba: {ex.Message}");
            }

            // ===== 5. CREAR CATEGORÍAS Y PRODUCTOS DE EJEMPLO =====
            try
            {
                Console.WriteLine("🔍 Verificando si existen categorías...");
                var categoriasExistentes = await context.Categorias.CountAsync();
                Console.WriteLine($"   Categorías existentes: {categoriasExistentes}");

                if (categoriasExistentes == 0)
                {
                    Console.WriteLine("📦 Creando categorías de ejemplo...");

                    var categorias = new List<Categoria>
                    {
                        // Bebidas
                        new Categoria { Tipo = TipoCategoria.Bebidas, Nombre = "Gaseosas", Orden = 1, Activo = true, FechaCreacion = DateTime.Now, CreadoPor = "Sistema" },
                        new Categoria { Tipo = TipoCategoria.Bebidas, Nombre = "Jugos", Orden = 2, Activo = true, FechaCreacion = DateTime.Now, CreadoPor = "Sistema" },
                        
                        // Cocina
                        new Categoria { Tipo = TipoCategoria.Cocina, Nombre = "Abarrotes", Orden = 1, Activo = true, FechaCreacion = DateTime.Now, CreadoPor = "Sistema" },
                        new Categoria { Tipo = TipoCategoria.Cocina, Nombre = "Carnes", Orden = 2, Activo = true, FechaCreacion = DateTime.Now, CreadoPor = "Sistema" },
                        
                        // Utensilios
                        new Categoria { Tipo = TipoCategoria.Utensilios, Nombre = "Cubiertos", Orden = 1, Activo = true, FechaCreacion = DateTime.Now, CreadoPor = "Sistema" },
                        new Categoria { Tipo = TipoCategoria.Utensilios, Nombre = "Vajilla", Orden = 2, Activo = true, FechaCreacion = DateTime.Now, CreadoPor = "Sistema" }
                    };

                    Console.WriteLine($"   Agregando {categorias.Count} categorías...");
                    context.Categorias.AddRange(categorias);
                    
                    Console.WriteLine("   Guardando categorías...");
                    await context.SaveChangesAsync();
                    Console.WriteLine($"✅ {categorias.Count} categorías creadas exitosamente");

                    // ===== CREAR PRODUCTOS =====
                    try
                    {
                        Console.WriteLine("📦 Creando productos de ejemplo...");

                        var catGaseosas = await context.Categorias.FirstOrDefaultAsync(c => c.Nombre == "Gaseosas");
                        var catJugos = await context.Categorias.FirstOrDefaultAsync(c => c.Nombre == "Jugos");
                        var catAbarrotes = await context.Categorias.FirstOrDefaultAsync(c => c.Nombre == "Abarrotes");
                        var catCarnes = await context.Categorias.FirstOrDefaultAsync(c => c.Nombre == "Carnes");

                        if (catGaseosas != null && catJugos != null && catAbarrotes != null && catCarnes != null)
                        {
                            var productos = new List<Producto>
                            {
                                new Producto { Codigo = "BEB-001", Nombre = "Coca Cola 2L", CategoriaId = catGaseosas.Id, Unidad = "Unidad", PrecioCompra = 4.50m, PrecioVenta = 6.00m, Descripcion = "Gaseosa Coca Cola 2 litros", Activo = true, FechaCreacion = DateTime.Now, CreadoPor = "Sistema" },
                                new Producto { Codigo = "BEB-002", Nombre = "Jugo Naranja Gloria 1L", CategoriaId = catJugos.Id, Unidad = "Unidad", PrecioCompra = 5.00m, PrecioVenta = 7.00m, Descripcion = "Jugo de naranja Gloria 1 litro", Activo = true, FechaCreacion = DateTime.Now, CreadoPor = "Sistema" },
                                new Producto { Codigo = "COC-001", Nombre = "Arroz Superior 1kg", CategoriaId = catAbarrotes.Id, Unidad = "Kilogramo", PrecioCompra = 3.50m, PrecioVenta = 4.50m, Descripcion = "Arroz superior extra", Activo = true, FechaCreacion = DateTime.Now, CreadoPor = "Sistema" },
                                new Producto { Codigo = "COC-002", Nombre = "Pollo Entero", CategoriaId = catCarnes.Id, Unidad = "Kilogramo", PrecioCompra = 8.00m, PrecioVenta = 12.00m, Descripcion = "Pollo fresco entero", Activo = true, FechaCreacion = DateTime.Now, CreadoPor = "Sistema" }
                            };

                            context.Productos.AddRange(productos);
                            await context.SaveChangesAsync();
                            Console.WriteLine($"✅ {productos.Count} productos creados exitosamente");
                        }
                    }
                    catch (Exception exProductos)
                    {
                        Console.WriteLine($"❌ Error al crear productos: {exProductos.Message}");
                    }

                    // ===== CREAR UTENSILIOS =====
                    try
                    {
                        Console.WriteLine("🍴 Creando utensilios de ejemplo...");

                        var catCubiertos = await context.Categorias.FirstOrDefaultAsync(c => c.Nombre == "Cubiertos");
                        var catVajilla = await context.Categorias.FirstOrDefaultAsync(c => c.Nombre == "Vajilla");

                        if (catCubiertos != null && catVajilla != null)
                        {
                            var utensilios = new List<Utensilio>
                            {
                                new Utensilio 
{ 
    Codigo = "CUB-001", 
    Nombre = "Cuchara de Mesa", 
    CategoriaId = catCubiertos.Id,  // ✅ Usar el Id de la categoría
    Unidad = "Unidad", 
    Precio = 2.50m, 
    Descripcion = "Cuchara de acero inoxidable", 
    Activo = true, 
    FechaCreacion = DateTime.Now, 
    CreadoPor = "Sistema" 
},
new Utensilio 
{ 
    Codigo = "VAJ-001", 
    Nombre = "Plato Hondo", 
    CategoriaId = catVajilla.Id,  // ✅ Usar el Id de la categoría
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
                    catch (Exception exUtensilios)
                    {
                        Console.WriteLine($"❌ Error al crear utensilios: {exUtensilios.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"ℹ️ Ya existen {categoriasExistentes} categorías, omitiendo seeding");
                }
            }
            catch (Exception exCategorias)
            {
                Console.WriteLine($"❌ ERROR CRÍTICO al crear categorías: {exCategorias.Message}");
                Console.WriteLine($"   StackTrace: {exCategorias.StackTrace}");
                if (exCategorias.InnerException != null)
                {
                    Console.WriteLine($"   Inner Exception: {exCategorias.InnerException.Message}");
                }
            }

            Console.WriteLine("🎉 Inicialización de base de datos completada");
        }

        private static async Task EnsureUserWithRole(
            UserManager<Usuario> userManager, 
            RoleManager<IdentityRole> roleManager, 
            string userName, 
            string nombreCompleto, 
            string roleName, 
            int localId, 
            string password)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole { Name = roleName, NormalizedName = roleName.ToUpper() });
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

            var create = await userManager.CreateAsync(nuevo, password);
            if (create.Succeeded)
            {
                await userManager.AddToRoleAsync(nuevo, roleName);
                Console.WriteLine($"✅ Usuario {roleName} creado: {userName} / {password}");
            }
            else
            {
                var errors = string.Join(", ", create.Errors.Select(e => e.Description));
                Console.WriteLine($"❌ Error al crear {userName}: {errors}");
            }
        }
    }
}