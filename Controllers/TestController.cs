using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Puerto92.Data;
using Puerto92.Models;

namespace Puerto92.Controllers
{
    public class TestController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public TestController(
            UserManager<Usuario> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // GET: /Test/CreateAdminMaestro
        public async Task<IActionResult> CreateAdminMaestro()
        {
            // Verificar si ya existe
            var existingUser = await _userManager.FindByNameAsync("admin");
            if (existingUser != null)
            {
                return Content("El usuario 'admin' ya existe. Ve a /Test/AsignarRol para asignarle el rol.");
            }

            // Obtener el primer local
            var local = await _context.Locales.FirstOrDefaultAsync();
            if (local == null)
            {
                return Content("Error: No hay locales. Créalos primero en DBeaver.");
            }

            // Crear usuario con ID personalizado
            var user = new Usuario
            {
                Id = "USR-001", // ID personalizado
                UserName = "admin",
                NombreCompleto = "Carlos Administrador Maestro",
                LocalId = local.Id,
                EsPrimerIngreso = false,
                Activo = true,
                FechaCreacion = DateTime.Now,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var result = await _userManager.CreateAsync(user, "Admin123");

            if (result.Succeeded)
            {
                // Asignar rol directamente con ID
                await _context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO aspnetuserroles (UserId, RoleId) VALUES ('USR-001', 'ROL-001')");

                return Content("✅ Usuario 'admin' creado con éxito. Usuario: admin | Contraseña: Admin123 | Rol: Admin Maestro. Ahora ELIMINA este TestController.");
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Content($"❌ Error: {errors}");
            }
        }

        // GET: /Test/AsignarRol
        public async Task<IActionResult> AsignarRol()
        {
            var user = await _userManager.FindByNameAsync("admin");
            if (user == null)
            {
                return Content("Error: Usuario 'admin' no existe.");
            }

            // Eliminar roles existentes
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }

            // Asignar rol Admin Maestro
            var role = await _roleManager.FindByIdAsync("ROL-001");
            if (role != null)
            {
                await _userManager.AddToRoleAsync(user, role.Name!);
                return Content($"✅ Rol 'Admin Maestro' asignado al usuario 'admin'. Cierra sesión y vuelve a iniciar.");
            }

            return Content("Error: Rol 'Admin Maestro' no encontrado.");
        }

        // GET: /Test/VerDatos
        public async Task<IActionResult> VerDatos()
        {
            var usuarios = await _context.Users.ToListAsync();
            var roles = await _roleManager.Roles.ToListAsync();

            var resultado = "<h2>USUARIOS</h2><ul>";
            foreach (var u in usuarios)
            {
                var userRoles = await _userManager.GetRolesAsync(u);
                resultado += $"<li>ID: {u.Id} | Usuario: {u.UserName} | Rol: {string.Join(", ", userRoles)}</li>";
            }
            resultado += "</ul>";

            resultado += "<h2>ROLES</h2><ul>";
            foreach (var r in roles)
            {
                resultado += $"<li>ID: {r.Id} | Nombre: {r.Name}</li>";
            }
            resultado += "</ul>";

            return Content(resultado, "text/html");
        }
    }
}