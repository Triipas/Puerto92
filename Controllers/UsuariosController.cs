using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Puerto92.Data;
using Puerto92.Models;
using Puerto92.ViewModels;
using Puerto92.Services;

namespace Puerto92.Controllers
{
    [Authorize(Roles = "Admin Maestro,Administrador Local")]
    public class UsuariosController : BaseController
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UsuariosController> _logger;
        private readonly IAuditService _auditService;

        public UsuariosController(
            UserManager<Usuario> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            ILogger<UsuariosController> logger,
            IAuditService auditService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _logger = logger;
            _auditService = auditService;
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

            foreach (var usuario in usuarios)
            {
                var user = await _userManager.FindByIdAsync(usuario.Id!);
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    usuario.NombreRol = roles.FirstOrDefault();
                }
            }

            // Automáticamente devuelve partial view si es AJAX
            return View(usuarios);
        }

        // GET: Usuarios/Create
        public async Task<IActionResult> Create()
        {
            await CargarListasDesplegables();
            return View();
        }

        // POST: Usuarios/Create (Método de respaldo, el modal usa CreateAjax)
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

            // Generar contraseña temporal usando el mismo algoritmo que resetear
            string passwordTemporal = GenerateTemporaryPassword();

            // Crear nuevo usuario
            var usuario = new Usuario
            {
                UserName = model.UserName,
                NombreCompleto = model.NombreCompleto,
                LocalId = model.LocalId,
                Activo = model.Activo,
                EsPrimerIngreso = true,     // Es su primera vez
                PasswordReseteada = false,   // No es un reseteo, es creación
                FechaCreacion = DateTime.Now
            };

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

                // NO mostrar contraseña en TempData (se usa modal ahora)
                TempData["Success"] = "Usuario creado exitosamente";
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

            ModelState.Remove("Password");

            if (!ModelState.IsValid)
            {
                if (IsAjaxRequest)
                    return JsonError("Datos inválidos. Por favor verifica los campos.");
                
                SetErrorMessage("Datos inválidos. Por favor verifica los campos.");
                return View(model); // ✅ Esto devolverá partial si es AJAX
            }

            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            var currentRoles = await _userManager.GetRolesAsync(usuario);
            var rolAnterior = currentRoles.FirstOrDefault() ?? "Sin rol";

            List<string> cambios = new List<string>();

            if (usuario.NombreCompleto != model.NombreCompleto)
                cambios.Add($"Nombre: '{usuario.NombreCompleto}' → '{model.NombreCompleto}'");

            if (usuario.UserName != model.UserName)
                cambios.Add($"Usuario: '{usuario.UserName}' → '{model.UserName}'");

            if (usuario.LocalId != model.LocalId)
            {
                var localAnterior = await _context.Locales.FindAsync(usuario.LocalId);
                var localNuevo = await _context.Locales.FindAsync(model.LocalId);
                cambios.Add($"Local: '{localAnterior?.Nombre}' → '{localNuevo?.Nombre}'");
            }

            if (usuario.Activo != model.Activo)
                cambios.Add($"Estado: {(usuario.Activo ? "Activo" : "Inactivo")} → {(model.Activo ? "Activo" : "Inactivo")}");

            usuario.NombreCompleto = model.NombreCompleto;
            usuario.UserName = model.UserName;
            usuario.LocalId = model.LocalId;
            usuario.Activo = model.Activo;

            var result = await _userManager.UpdateAsync(usuario);

            if (result.Succeeded)
            {
                await _userManager.RemoveFromRolesAsync(usuario, currentRoles);
                var newRole = await _roleManager.FindByIdAsync(model.RolId);
                string rolNuevo = "Sin rol";

                if (newRole != null)
                {
                    await _userManager.AddToRoleAsync(usuario, newRole.Name!);
                    rolNuevo = newRole.Name!;
                }

                if (rolAnterior != rolNuevo)
                {
                    cambios.Add($"Rol: '{rolAnterior}' → '{rolNuevo}'");
                    await _auditService.RegistrarCambioRolAsync(
                        usuario: usuario.UserName!,
                        rolAnterior: rolAnterior,
                        rolNuevo: rolNuevo);
                }

                _logger.LogInformation($"Usuario {usuario.UserName} editado por {User.Identity!.Name}");

                if (cambios.Any())
                {
                    await _auditService.RegistrarEdicionUsuarioAsync(
                        usuarioEditado: usuario.UserName!,
                        cambiosRealizados: string.Join(", ", cambios));
                }

                // ✅ Usar el método helper de BaseController
                SetSuccessMessage("Usuario actualizado exitosamente");
                return RedirectToActionAjax(nameof(Index));
            }

            if (IsAjaxRequest)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return JsonError(errors);
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            SetErrorMessage("Error al actualizar el usuario");
            return View(model); // Devolverá partial si es AJAX
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

        // POST: Usuarios/CreateAjax (AJAX)
        [HttpPost]
        public async Task<IActionResult> CreateAjax(UsuarioViewModel model)
        {
            try
            {
                ModelState.Remove("Password");

                if (!ModelState.IsValid)
                {
                    return JsonError("Datos inválidos. Por favor verifica los campos.");
                }

                var existingUser = await _userManager.FindByNameAsync(model.UserName);
                if (existingUser != null)
                {
                    return JsonError("El nombre de usuario ya existe");
                }

                string passwordTemporal = GenerateTemporaryPassword();
                var local = await _context.Locales.FindAsync(model.LocalId);
                var nombreLocal = local?.Nombre ?? "Desconocido";

                var usuario = new Usuario
                {
                    UserName = model.UserName,
                    NombreCompleto = model.NombreCompleto,
                    LocalId = model.LocalId,
                    Activo = model.Activo,
                    EsPrimerIngreso = true,
                    PasswordReseteada = false,
                    FechaCreacion = DateTime.Now
                };

                var result = await _userManager.CreateAsync(usuario, passwordTemporal);

                if (result.Succeeded)
                {
                    var role = await _roleManager.FindByIdAsync(model.RolId);
                    string rolNombre = "Sin rol";

                    if (role != null)
                    {
                        await _userManager.AddToRoleAsync(usuario, role.Name!);
                        rolNombre = role.Name!;
                    }

                    _logger.LogInformation($"Usuario {usuario.UserName} creado por {User.Identity!.Name}");

                    await _auditService.RegistrarCreacionUsuarioAsync(
                        usuarioCreado: usuario.UserName!,
                        rol: rolNombre,
                        local: nombreLocal);

                    // ✅ Usar el método helper de BaseController
                    return JsonSuccess(
                        "Usuario creado exitosamente",
                        data: new
                        {
                            password = passwordTemporal,
                            nombreCompleto = usuario.NombreCompleto,
                            userName = usuario.UserName,
                            rolNombre = rolNombre
                        }
                    );
                }

                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return JsonError($"Error al crear usuario: {errors}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear usuario");
                
                await _auditService.RegistrarErrorSistemaAsync(
                    error: "Error al crear usuario",
                    detalles: ex.Message);

                return JsonError("Error al crear el usuario. Por favor intenta nuevamente.");
            }
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
                
                // 🔍 REGISTRAR ELIMINACIÓN DE USUARIO EN AUDITORÍA
                await _auditService.RegistrarEliminacionUsuarioAsync(
                    usuarioEliminado: usuario.UserName!);

                TempData["Success"] = "Usuario eliminado exitosamente";
            }
            else
            {
                TempData["Error"] = "Error al eliminar el usuario";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Usuarios/ResetPassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id, [FromForm] string tempPassword)
        {
            try
            {
                var usuario = await _userManager.FindByIdAsync(id);
                if (usuario == null)
                {
                    TempData["Error"] = "Usuario no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                // Generar token de reseteo de contraseña
                var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);

                // Si no se proporciona contraseña temporal desde el formulario, generarla
                if (string.IsNullOrEmpty(tempPassword))
                {
                    tempPassword = GenerateTemporaryPassword();
                }

                // Resetear la contraseña
                var result = await _userManager.ResetPasswordAsync(usuario, token, tempPassword);

                if (result.Succeeded)
                {
                    // Marcar que la contraseña fue reseteada (NO es primer ingreso)
                    usuario.PasswordReseteada = true;
                    usuario.EsPrimerIngreso = false; // Ya no es primer ingreso, es un reseteo
                    await _userManager.UpdateAsync(usuario);

                    _logger.LogWarning($"Contraseña reseteada para usuario {usuario.UserName} por {User.Identity!.Name}");

                    // 🔍 REGISTRAR RESET DE CONTRASEÑA EN AUDITORÍA
                    await _auditService.RegistrarResetPasswordAsync(
                        usuarioAfectado: usuario.UserName!);

                    // Mensaje genérico sin mostrar la contraseña (ya está en el modal)
                    TempData["Success"] = $"Contraseña reseteada exitosamente para {usuario.NombreCompleto}";

                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    TempData["Error"] = $"Error al resetear contraseña: {errors}";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al resetear contraseña");
                
                // 🔍 REGISTRAR ERROR EN AUDITORÍA
                await _auditService.RegistrarErrorSistemaAsync(
                    error: "Error al resetear contraseña",
                    detalles: ex.Message);

                TempData["Error"] = "Error al resetear la contraseña. Por favor intenta nuevamente.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Método auxiliar para generar contraseña temporal
        private string GenerateTemporaryPassword()
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