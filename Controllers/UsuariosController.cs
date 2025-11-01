using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Puerto92.Data;
using Puerto92.Models;
using Puerto92.ViewModels;

namespace Puerto92.Controllers
{
    [Authorize(Roles = "Admin Maestro,Administrador Local")]
    public class UsuariosController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UsuariosController> _logger;

        public UsuariosController(
            UserManager<Usuario> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            ILogger<UsuariosController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _logger = logger;
        }

        // GET: Usuarios
        public async Task<IActionResult> Index()
        {
            var usuarios = await _context.Users
                .Include(u => u.Local)
                .Select(u => new UsuarioViewModel
                {
                    Id = u.Id,
                    NombreCompleto = u.NombreCompleto,
                    UserName = u.UserName!,
                    LocalId = u.LocalId,
                    NombreLocal = u.Local!.Nombre,
                    Activo = u.Activo,
                    UltimoAcceso = u.UltimoAcceso
                })
                .ToListAsync();

            // Obtener roles de cada usuario
            foreach (var usuario in usuarios)
            {
                var user = await _userManager.FindByIdAsync(usuario.Id!);
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    usuario.NombreRol = roles.FirstOrDefault();
                }
            }

            return View(usuarios);
        }

        // GET: Usuarios/Create
        public async Task<IActionResult> Create()
        {
            await CargarListasDesplegables();
            return View();
        }

        // POST: Usuarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UsuarioViewModel model)
        {
            // Remover validación de contraseña (se generará automáticamente)
            ModelState.Remove("Password");

            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Index));
            }

            // Verificar si el usuario ya existe
            var existingUser = await _userManager.FindByNameAsync(model.UserName);
            if (existingUser != null)
            {
                TempData["Error"] = "El nombre de usuario ya existe";
                return RedirectToAction(nameof(Index));
            }

            // Crear nuevo usuario
            var usuario = new Usuario
            {
                UserName = model.UserName,
                NombreCompleto = model.NombreCompleto,
                LocalId = model.LocalId,
                Activo = model.Activo,
                EsPrimerIngreso = true, // Forzar cambio de contraseña
                FechaCreacion = DateTime.Now
            };

            // Generar contraseña temporal
            string passwordTemporal = $"Temp{DateTime.Now.Year}!";

            var result = await _userManager.CreateAsync(usuario, passwordTemporal);

            if (result.Succeeded)
            {
                // Asignar rol
                var role = await _roleManager.FindByIdAsync(model.RolId);
                if (role != null)
                {
                    await _userManager.AddToRoleAsync(usuario, role.Name!);
                }

                _logger.LogInformation($"Usuario {usuario.UserName} creado por {User.Identity!.Name}");
                TempData["Success"] = $"Usuario creado. Contraseña temporal: {passwordTemporal}";
                return RedirectToAction(nameof(Index));
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            TempData["Error"] = $"Error al crear usuario: {errors}";
            return RedirectToAction(nameof(Index));
        }

        // POST: Usuarios/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            usuario.Activo = !usuario.Activo;
            await _userManager.UpdateAsync(usuario);

            var estado = usuario.Activo ? "activado" : "desactivado";
            _logger.LogInformation($"Usuario {usuario.UserName} {estado} por {User.Identity!.Name}");
            TempData["Success"] = $"Usuario {estado} exitosamente";

            return RedirectToAction(nameof(Index));
        }

        // Método auxiliar para cargar listas desplegables
        private async Task CargarListasDesplegables()
        {
            ViewBag.Roles = await _roleManager.Roles
                .Select(r => new SelectListItem
                {
                    Value = r.Id,
                    Text = r.Name
                })
                .ToListAsync();

            ViewBag.Locales = await _context.Locales
                .Where(l => l.Activo)
                .Select(l => new SelectListItem
                {
                    Value = l.Id.ToString(),
                    Text = l.Nombre
                })
                .ToListAsync();
        }

        // GET: Usuarios/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(usuario);
            var roleName = roles.FirstOrDefault();
            var role = roleName != null ? await _roleManager.FindByNameAsync(roleName) : null;

            var model = new UsuarioViewModel
            {
                Id = usuario.Id,
                NombreCompleto = usuario.NombreCompleto,
                UserName = usuario.UserName!,
                LocalId = usuario.LocalId,
                RolId = role?.Id ?? string.Empty,
                Activo = usuario.Activo
            };

            await CargarListasDesplegables();
            return View(model);
        }

        // POST: Usuarios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UsuarioViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            // Remover validación de contraseña si está vacía
            if (string.IsNullOrEmpty(model.Password))
            {
                ModelState.Remove("Password");
            }

            if (!ModelState.IsValid)
            {
                await CargarListasDesplegables();
                return View(model);
            }

            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            // Actualizar datos
            usuario.NombreCompleto = model.NombreCompleto;
            usuario.UserName = model.UserName;
            usuario.LocalId = model.LocalId;
            usuario.Activo = model.Activo;

            var result = await _userManager.UpdateAsync(usuario);

            if (result.Succeeded)
            {
                // Actualizar rol
                var currentRoles = await _userManager.GetRolesAsync(usuario);
                await _userManager.RemoveFromRolesAsync(usuario, currentRoles);

                var newRole = await _roleManager.FindByIdAsync(model.RolId);
                if (newRole != null)
                {
                    await _userManager.AddToRoleAsync(usuario, newRole.Name!);
                }

                // Cambiar contraseña si se proporcionó una nueva
                if (!string.IsNullOrEmpty(model.Password))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
                    await _userManager.ResetPasswordAsync(usuario, token, model.Password);
                }

                _logger.LogInformation($"Usuario {usuario.UserName} editado por {User.Identity!.Name}");
                TempData["Success"] = "Usuario actualizado exitosamente";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await CargarListasDesplegables();
            return View(model);
        }
        // GET: /Usuarios/GetRolesYLocales (Para AJAX)
        [HttpGet]
        public async Task<IActionResult> GetRolesYLocales()
        {
            var roles = await _roleManager.Roles
                .Select(r => new { value = r.Id, text = r.Name })
                .ToListAsync();

            var locales = await _context.Locales
                .Where(l => l.Activo)
                .Select(l => new { value = l.Id, text = l.Nombre })
                .ToListAsync();

            return Json(new { roles, locales });
        }

        // GET: /Usuarios/GetUsuario?id=xxx (Para AJAX)
        [HttpGet]
        public async Task<IActionResult> GetUsuario(string id)
        {
            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(usuario);
            var roleName = roles.FirstOrDefault();
            var role = roleName != null ? await _roleManager.FindByNameAsync(roleName) : null;

            var data = new
            {
                id = usuario.Id,
                nombreCompleto = usuario.NombreCompleto,
                userName = usuario.UserName,
                rolId = role?.Id ?? string.Empty,
                localId = usuario.LocalId,
                activo = usuario.Activo
            };

            return Json(data);
        }

        // POST: /Usuarios/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            var result = await _userManager.DeleteAsync(usuario);

            if (result.Succeeded)
            {
                _logger.LogInformation($"Usuario {usuario.UserName} eliminado por {User.Identity!.Name}");
                TempData["Success"] = "Usuario eliminado exitosamente";
            }
            else
            {
                TempData["Error"] = "Error al eliminar el usuario";
            }

            return RedirectToAction(nameof(Index));
        }
    }

}